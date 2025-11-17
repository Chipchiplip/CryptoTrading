using CryptoTrading.Models.Market;
using CryptoTrading.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Market;

/// <summary>
/// Validates price data quality, detects anomalies, and filters outliers
/// </summary>
public class PriceValidator : IPriceValidator
{
    private readonly ILogger<PriceValidator> _logger;
    private readonly ICryptoCacheService _cache;
    private readonly decimal _maxDeviationPercent;
    private readonly TimeSpan _staleDataThreshold;
    private readonly decimal _minVolume;
    private readonly decimal _outlierZScore;

    private const string CACHE_KEY_PREFIX = "price_history:";
    private const int MAX_HISTORY_SIZE = 100;

    public PriceValidator(
        ILogger<PriceValidator> logger,
        ICryptoCacheService cache,
        IConfiguration configuration)
    {
        _logger = logger;
        _cache = cache;

        // Load configuration with defaults
        _maxDeviationPercent = decimal.Parse(
            configuration["Trading:PriceValidation:MaxDeviationPercent"] ?? "5.0");
        _staleDataThreshold = TimeSpan.FromMinutes(double.Parse(
            configuration["Trading:PriceValidation:StaleDataThresholdMinutes"] ?? "5"));
        _minVolume = decimal.Parse(
            configuration["Trading:PriceValidation:MinVolume"] ?? "1000");
        _outlierZScore = decimal.Parse(
            configuration["Trading:PriceValidation:OutlierZScore"] ?? "3.0");
    }

    public async Task<PriceValidationResult> ValidatePriceAsync(
        string symbol,
        decimal price,
        DateTime timestamp,
        decimal? volume = null)
    {
        var result = new PriceValidationResult { IsValid = true };
        var metrics = result.Metrics;

        // 1. Validate price is positive
        if (price <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Price must be greater than zero");
            return result;
        }

        // 2. Check for stale data
        if (IsStaleData(timestamp))
        {
            metrics.DataAge = DateTime.UtcNow - timestamp;
            result.Warnings.Add($"Price data is stale ({metrics.DataAge.Value.TotalMinutes:F1} minutes old)");

            if (metrics.DataAge.Value > TimeSpan.FromMinutes(15))
            {
                result.IsValid = false;
                result.Errors.Add("Price data is too stale to use");
                return result;
            }
        }

        // 3. Check volume if provided
        if (volume.HasValue)
        {
            if (volume.Value < _minVolume)
            {
                result.Warnings.Add($"Volume ({volume.Value:F2}) is below minimum threshold ({_minVolume})");
            }
        }

        // 4. Check deviation from previous price
        var previousPrice = await GetPreviousPriceAsync(symbol);
        if (previousPrice.HasValue)
        {
            var deviationPercent = Math.Abs((price - previousPrice.Value) / previousPrice.Value) * 100;
            metrics.DeviationPercent = deviationPercent;

            if (deviationPercent > _maxDeviationPercent)
            {
                result.Warnings.Add(
                    $"Price deviation ({deviationPercent:F2}%) exceeds threshold ({_maxDeviationPercent}%)");

                // If deviation is extreme (2x threshold), reject
                if (deviationPercent > _maxDeviationPercent * 2)
                {
                    result.IsValid = false;
                    result.Errors.Add("Price deviation is too extreme, likely data error");
                    return result;
                }
            }
        }

        // 5. Store price in history for future validation
        await StorePriceInHistoryAsync(symbol, price, timestamp);

        return result;
    }

    public async Task<PriceValidationResult> ValidateCandleAsync(
        string symbol,
        OHLCVCandle candle)
    {
        var result = new PriceValidationResult { IsValid = true };

        // 1. Validate OHLC relationship
        if (candle.High < candle.Low)
        {
            result.IsValid = false;
            result.Errors.Add("High price cannot be less than low price");
            return result;
        }

        if (candle.Open < candle.Low || candle.Open > candle.High)
        {
            result.IsValid = false;
            result.Errors.Add("Open price must be within high-low range");
            return result;
        }

        if (candle.Close < candle.Low || candle.Close > candle.High)
        {
            result.IsValid = false;
            result.Errors.Add("Close price must be within high-low range");
            return result;
        }

        // 2. Validate individual prices
        var closeValidation = await ValidatePriceAsync(
            symbol,
            candle.Close,
            candle.Timestamp,
            candle.Volume);

        if (!closeValidation.IsValid)
        {
            result.IsValid = false;
            result.Errors.AddRange(closeValidation.Errors);
        }

        result.Warnings.AddRange(closeValidation.Warnings);
        result.Metrics = closeValidation.Metrics;

        // 3. Check for suspicious wicks (> 20% of price)
        var wickSize = candle.High - candle.Low;
        var wickPercent = (wickSize / candle.Close) * 100;
        if (wickPercent > 20)
        {
            result.Warnings.Add($"Large wick detected ({wickPercent:F2}%), possible manipulation");
        }

        return result;
    }

    public Task<bool> IsDeviationAcceptableAsync(
        string symbol,
        decimal currentPrice,
        decimal previousPrice)
    {
        if (previousPrice <= 0)
            return Task.FromResult(true); // No baseline to compare

        var deviationPercent = Math.Abs((currentPrice - previousPrice) / previousPrice) * 100;
        return Task.FromResult(deviationPercent <= _maxDeviationPercent);
    }

    public bool IsStaleData(DateTime timestamp)
    {
        var age = DateTime.UtcNow - timestamp;
        return age > _staleDataThreshold;
    }

    public Task<List<OHLCVCandle>> FilterOutliersAsync(
        string symbol,
        List<OHLCVCandle> candles)
    {
        if (candles.Count < 10)
            return Task.FromResult(candles); // Not enough data for statistical analysis

        // Calculate mean and standard deviation of close prices
        var closePrices = candles.Select(c => (double)c.Close).ToList();
        var mean = closePrices.Average();
        var variance = closePrices.Select(x => Math.Pow(x - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        var filtered = new List<OHLCVCandle>();
        var outlierCount = 0;

        foreach (var candle in candles)
        {
            var zScore = Math.Abs(((double)candle.Close - mean) / stdDev);

            if (zScore <= (double)_outlierZScore)
            {
                filtered.Add(candle);
            }
            else
            {
                outlierCount++;
                _logger.LogWarning(
                    "Filtered outlier candle for {Symbol} at {Timestamp}: Price={Price}, Z-Score={ZScore:F2}",
                    symbol, candle.Timestamp, candle.Close, zScore);
            }
        }

        if (outlierCount > 0)
        {
            _logger.LogInformation(
                "Filtered {Count} outlier candles out of {Total} for {Symbol}",
                outlierCount, candles.Count, symbol);
        }

        return Task.FromResult(filtered);
    }

    private Task<decimal?> GetPreviousPriceAsync(string symbol)
    {
        try
        {
            // NOTE: ICryptoCacheService doesn't have generic GetAsync method
            // TODO: Use IMemoryCache directly or extend ICryptoCacheService
            // For now, cache lookup is disabled
            var history = (List<PricePoint>?)null;
            /*
            var key = $"{CACHE_KEY_PREFIX}{symbol}";
            var history = await _cache.GetAsync<List<PricePoint>>(key);
            */

            if (history != null && history.Count > 0)
            {
                // Return most recent price
                return Task.FromResult<decimal?>(history.OrderByDescending(p => p.Timestamp).First().Price);
            }

            return Task.FromResult<decimal?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve previous price for {Symbol}", symbol);
            return Task.FromResult<decimal?>(null);
        }
    }

    private async Task StorePriceInHistoryAsync(string symbol, decimal price, DateTime timestamp)
    {
        try
        {
            // NOTE: ICryptoCacheService doesn't have generic GetAsync/SetAsync methods
            // TODO: Use IMemoryCache directly or extend ICryptoCacheService
            // For now, price history caching is disabled
            await Task.CompletedTask;
            /*
            var key = $"{CACHE_KEY_PREFIX}{symbol}";
            var history = await _cache.GetAsync<List<PricePoint>>(key) ?? new List<PricePoint>();

            // Add new price point
            history.Add(new PricePoint
            {
                Price = price,
                Timestamp = timestamp
            });

            // Keep only recent history
            if (history.Count > MAX_HISTORY_SIZE)
            {
                history = history
                    .OrderByDescending(p => p.Timestamp)
                    .Take(MAX_HISTORY_SIZE)
                    .ToList();
            }

            // Store back with 1 hour TTL
            await _cache.SetAsync(key, history, TimeSpan.FromHours(1));
            */
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store price history for {Symbol}", symbol);
        }
    }

    private class PricePoint
    {
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
