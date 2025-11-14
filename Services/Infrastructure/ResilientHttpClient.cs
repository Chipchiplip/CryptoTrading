using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimit;
using Polly.Retry;
using Polly.Timeout;
using System.Net;

namespace CryptoTrading.Services.Infrastructure
{
    /// <summary>
    /// Resilient HTTP client with Polly policies for retry, circuit breaker, timeout, and rate limiting
    /// </summary>
    public interface IResilientHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken = default);
        Task<T?> GetJsonAsync<T>(string uri, CancellationToken cancellationToken = default);
    }

    public class ResilientHttpClient : IResilientHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ResilientHttpClient> _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

        public ResilientHttpClient(
            HttpClient httpClient,
            ILogger<ResilientHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _resiliencePolicy = BuildResiliencePolicy();
        }

        public async Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken = default)
        {
            return await _resiliencePolicy.ExecuteAsync(async (ct) =>
            {
                _logger.LogDebug("Making HTTP GET request to: {Uri}", uri);
                var response = await _httpClient.GetAsync(uri, ct);
                
                // Log rate limit headers if present
                if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining))
                {
                    _logger.LogDebug("Rate limit remaining: {Remaining}", remaining.FirstOrDefault());
                }

                response.EnsureSuccessStatusCode();
                return response;
            }, cancellationToken);
        }

        public async Task<T?> GetJsonAsync<T>(string uri, CancellationToken cancellationToken = default)
        {
            var response = await GetAsync(uri, cancellationToken);
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }

        private IAsyncPolicy<HttpResponseMessage> BuildResiliencePolicy()
        {
            // 1. Retry policy with exponential backoff and jitter
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TimeoutException>()
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests || 
                              r.StatusCode == HttpStatusCode.ServiceUnavailable ||
                              (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: (retryAttempt, response, context) =>
                    {
                        // Exponential backoff with jitter
                        var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                        
                        // Check for Retry-After header (rate limiting)
                        if (response.Result?.Headers.RetryAfter?.Delta.HasValue == true)
                        {
                            return response.Result.Headers.RetryAfter.Delta.Value + jitter;
                        }
                        
                        return baseDelay + jitter;
                    },
                    onRetryAsync: (outcome, timespan, retryAttempt, context) =>
                    {
                        _logger.LogWarning("Retry {RetryAttempt} after {Delay}ms due to: {Reason}",
                            retryAttempt, timespan.TotalMilliseconds,
                            outcome.Exception?.Message ?? $"Status: {outcome.Result?.StatusCode}");
                        return Task.CompletedTask;
                    });

            // 2. Circuit breaker policy
            var circuitBreakerPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TimeoutException>()
                .OrResult(r => (int)r.StatusCode >= 500)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, breakDelay) =>
                    {
                        _logger.LogError("Circuit breaker opened for {Delay}s due to: {Reason}",
                            breakDelay.TotalSeconds,
                            outcome.Exception?.Message ?? $"Status: {outcome.Result?.StatusCode}");
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit breaker half-open, testing...");
                    });

            // 3. Timeout policy
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(10),
                onTimeoutAsync: (context, timespan, task) =>
                {
                    _logger.LogWarning("Request timeout after {Timeout}s", timespan.TotalSeconds);
                    return Task.CompletedTask;
                });

            // Combine policies: Timeout -> Retry -> Circuit Breaker
            return Policy.WrapAsync(circuitBreakerPolicy, retryPolicy, timeoutPolicy);
        }
    }

    /// <summary>
    /// Token bucket rate limiter for API calls
    /// </summary>
    public interface IRateLimiter
    {
        Task<bool> TryAcquireAsync(string key, CancellationToken cancellationToken = default);
        Task WaitAsync(string key, CancellationToken cancellationToken = default);
    }

    public class TokenBucketRateLimiter : IRateLimiter
    {
        private readonly Dictionary<string, TokenBucket> _buckets = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ILogger<TokenBucketRateLimiter> _logger;

        // Default: 10 requests per second
        private readonly int _capacity;
        private readonly TimeSpan _refillInterval;
        private readonly int _tokensPerInterval;

        public TokenBucketRateLimiter(
            ILogger<TokenBucketRateLimiter> logger,
            int capacity = 10,
            int tokensPerSecond = 10)
        {
            _logger = logger;
            _capacity = capacity;
            _refillInterval = TimeSpan.FromSeconds(1);
            _tokensPerInterval = tokensPerSecond;
        }

        public async Task<bool> TryAcquireAsync(string key, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (!_buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new TokenBucket(_capacity, _tokensPerInterval, _refillInterval);
                    _buckets[key] = bucket;
                }

                return bucket.TryConsume();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task WaitAsync(string key, CancellationToken cancellationToken = default)
        {
            while (!await TryAcquireAsync(key, cancellationToken))
            {
                _logger.LogDebug("Rate limit reached for {Key}, waiting...", key);
                await Task.Delay(100, cancellationToken); // Wait 100ms between attempts
            }
        }

        private class TokenBucket
        {
            private readonly int _capacity;
            private readonly int _tokensPerInterval;
            private readonly TimeSpan _refillInterval;
            private int _tokens;
            private DateTime _lastRefill;

            public TokenBucket(int capacity, int tokensPerInterval, TimeSpan refillInterval)
            {
                _capacity = capacity;
                _tokensPerInterval = tokensPerInterval;
                _refillInterval = refillInterval;
                _tokens = capacity;
                _lastRefill = DateTime.UtcNow;
            }

            public bool TryConsume()
            {
                Refill();

                if (_tokens > 0)
                {
                    _tokens--;
                    return true;
                }

                return false;
            }

            private void Refill()
            {
                var now = DateTime.UtcNow;
                var timeSinceRefill = now - _lastRefill;

                if (timeSinceRefill >= _refillInterval)
                {
                    var intervalsElapsed = (int)(timeSinceRefill / _refillInterval);
                    var tokensToAdd = intervalsElapsed * _tokensPerInterval;
                    _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
                    _lastRefill = now;
                }
            }
        }
    }
}

