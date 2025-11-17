using System;
using System.Collections.Generic;
using System.Diagnostics;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Services;
using CryptoTrading.Services.Market;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Enhanced market data provider with exchange integration and price validation
    /// </summary>
    public class MarketDataProvider : IMarketDataProvider
    {
        private readonly IExchangeDataProvider _exchangeDataProvider;
        private readonly IPriceValidator _priceValidator;
        private readonly ICryptoCacheService _cacheService;
        private readonly ILogger<MarketDataProvider> _logger;
        private readonly IConfiguration _configuration;

        private const int MAX_RETRIES = 3;
        private const int BASE_DELAY_MS = 100;

        public MarketDataProvider(
            IExchangeDataProvider exchangeDataProvider,
            IPriceValidator priceValidator,
            ICryptoCacheService cacheService,
            ILogger<MarketDataProvider> logger,
            IConfiguration configuration)
        {
            _exchangeDataProvider = exchangeDataProvider;
            _priceValidator = priceValidator;
            _cacheService = cacheService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<decimal> GetMidPriceAsync(
            string baseAsset,
            string quoteAsset,
            CancellationToken cancellationToken = default)
        {
            var symbol = $"{baseAsset}{quoteAsset}";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Use exponential backoff retry logic
                decimal? price = null;
                Exception? lastException = null;

                for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
                {
                    try
                    {
                        // Get order book mid price (best bid + best ask) / 2
                        price = await _exchangeDataProvider.GetOrderBookMidPriceAsync(symbol, cancellationToken);

                        if (price.HasValue && price.Value > 0)
                        {
                            // Validate price quality
                            var validation = await _priceValidator.ValidatePriceAsync(
                                symbol,
                                price.Value,
                                DateTime.UtcNow);

                            if (validation.IsValid)
                            {
                                stopwatch.Stop();
                                _logger.LogDebug(
                                    "Fetched mid price for {Symbol}: {Price} (latency: {Latency}ms, exchange: {Exchange})",
                                    symbol, price.Value, stopwatch.ElapsedMilliseconds, _exchangeDataProvider.ExchangeName);

                                // Cache the price
                                await CachePriceAsync(symbol, price.Value);

                                return price.Value;
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Price validation failed for {Symbol}: {Errors}",
                                    symbol, string.Join(", ", validation.Errors));

                                // If validation fails but it's not critical, still return the price
                                if (validation.Errors.Count == 0 && validation.Warnings.Count > 0)
                                {
                                    return price.Value;
                                }
                            }
                        }

                        break; // If we got here, no need to retry
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(
                            ex,
                            "Attempt {Attempt}/{MaxRetries} failed to fetch price for {Symbol}",
                            attempt + 1, MAX_RETRIES, symbol);

                        if (attempt < MAX_RETRIES - 1)
                        {
                            // Exponential backoff: 100ms, 200ms, 400ms
                            var delay = BASE_DELAY_MS * (int)Math.Pow(2, attempt);
                            await Task.Delay(delay, cancellationToken);
                        }
                    }
                }

                // If all retries failed, try to get cached price
                var cachedPrice = await GetCachedPriceAsync(symbol);
                if (cachedPrice.HasValue)
                {
                    _logger.LogWarning(
                        "Using cached price for {Symbol} after {Attempts} failed attempts: {Price}",
                        symbol, MAX_RETRIES, cachedPrice.Value);
                    return cachedPrice.Value;
                }

                // DEMO FIX: Return 0 and log error - strategies should validate price > 0 before using
                _logger.LogError(
                    lastException,
                    "Failed to fetch price for {Symbol} after {Attempts} attempts and no cache available",
                    symbol, MAX_RETRIES);

                // Return 0 to indicate failure - calling code must validate price > 0
                return 0m;
            }
            finally
            {
                stopwatch.Stop();

                // Monitor for excessive latency
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning(
                        "High latency detected for {Symbol}: {Latency}ms",
                        symbol, stopwatch.ElapsedMilliseconds);
                }
            }
        }

        public async Task<List<OhlcvData>> GetOhlcvAsync(
            string baseAsset,
            string quoteAsset,
            DateTime startDate,
            DateTime endDate,
            string interval = "1h",
            CancellationToken cancellationToken = default)
        {
            if (endDate <= startDate)
            {
                return new List<OhlcvData>();
            }

            var symbol = $"{baseAsset}{quoteAsset}";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Calculate number of candles needed
                var timeSpan = endDate - startDate;
                var intervalMinutes = ParseIntervalToMinutes(interval);
                var limit = (int)Math.Ceiling(timeSpan.TotalMinutes / intervalMinutes);
                limit = Math.Min(limit, 1000); // Cap at 1000 candles

                // Fetch candles from exchange
                var exchangeCandles = await _exchangeDataProvider.GetCandlesAsync(
                    symbol,
                    interval,
                    limit,
                    cancellationToken);

                if (exchangeCandles == null || exchangeCandles.Count == 0)
                {
                    _logger.LogWarning("No candles returned for {Symbol}", symbol);
                    return new List<OhlcvData>();
                }

                // Filter outliers
                var filtered = await _priceValidator.FilterOutliersAsync(symbol, exchangeCandles);

                // Validate each candle
                var validatedCandles = new List<OhlcvData>();
                foreach (var candle in filtered)
                {
                    var validation = await _priceValidator.ValidateCandleAsync(symbol, candle);

                    if (validation.IsValid || validation.Errors.Count == 0)
                    {
                        validatedCandles.Add(new OhlcvData
                        {
                            Timestamp = candle.Timestamp,
                            Open = candle.Open,
                            High = candle.High,
                            Low = candle.Low,
                            Close = candle.Close,
                            Volume = candle.Volume
                        });
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Skipping invalid candle for {Symbol} at {Timestamp}: {Errors}",
                            symbol, candle.Timestamp, string.Join(", ", validation.Errors));
                    }
                }

                // Filter by date range
                var result = validatedCandles
                    .Where(c => c.Timestamp >= startDate && c.Timestamp <= endDate)
                    .OrderBy(c => c.Timestamp)
                    .ToList();

                stopwatch.Stop();
                _logger.LogDebug(
                    "Fetched {Count} OHLCV candles for {Symbol} (latency: {Latency}ms)",
                    result.Count, symbol, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve OHLCV data for {Asset}/{Quote}", baseAsset, quoteAsset);
                return new List<OhlcvData>();
            }
        }

        private static int ParseIntervalToMinutes(string interval)
        {
            return interval.ToLowerInvariant() switch
            {
                "1m" or "1min" => 1,
                "5m" or "5min" => 5,
                "15m" or "15min" => 15,
                "30m" or "30min" => 30,
                "1h" or "1hour" => 60,
                "4h" or "4hour" => 240,
                "1d" or "1day" => 1440,
                _ => 60
            };
        }

        private async Task CachePriceAsync(string symbol, decimal price)
        {
            // NOTE: ICryptoCacheService doesn't have generic SetAsync method
            // TODO: Use IMemoryCache directly or extend ICryptoCacheService
            // For now, caching is disabled
            await Task.CompletedTask;
            /*
            try
            {
                var cacheKey = $"price:{symbol}";
                await _cacheService.SetAsync(cacheKey, price, TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache price for {Symbol}", symbol);
            }
            */
        }

        private async Task<decimal?> GetCachedPriceAsync(string symbol)
        {
            // NOTE: ICryptoCacheService doesn't have generic GetAsync method
            // TODO: Use IMemoryCache directly or extend ICryptoCacheService
            // For now, caching is disabled
            await Task.CompletedTask;
            return null;
            /*
            try
            {
                var cacheKey = $"price:{symbol}";
                return await _cacheService.GetAsync<decimal?>(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve cached price for {Symbol}", symbol);
                return null;
            }
            */
        }
    }
}

