using CryptoTradingApp.Models.Market;

namespace CryptoTradingApp.Services.Market;

/// <summary>
/// Interface for validating price data quality and detecting anomalies
/// </summary>
public interface IPriceValidator
{
    /// <summary>
    /// Validates a single price point
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="price">Current price</param>
    /// <param name="timestamp">Price timestamp</param>
    /// <param name="volume">Trading volume (optional)</param>
    /// <returns>Validation result with details</returns>
    Task<PriceValidationResult> ValidatePriceAsync(
        string symbol,
        decimal price,
        DateTime timestamp,
        decimal? volume = null);

    /// <summary>
    /// Validates OHLCV candle data
    /// </summary>
    Task<PriceValidationResult> ValidateCandleAsync(
        string symbol,
        OHLCVCandle candle);

    /// <summary>
    /// Checks if price deviation from previous tick exceeds threshold
    /// </summary>
    Task<bool> IsDeviationAcceptableAsync(
        string symbol,
        decimal currentPrice,
        decimal previousPrice);

    /// <summary>
    /// Detects if price data is stale
    /// </summary>
    bool IsStaleData(DateTime timestamp);

    /// <summary>
    /// Filters outlier candles from a series
    /// </summary>
    Task<List<OHLCVCandle>> FilterOutliersAsync(
        string symbol,
        List<OHLCVCandle> candles);
}

/// <summary>
/// Result of price validation
/// </summary>
public class PriceValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public ValidationMetrics Metrics { get; set; } = new();

    public static PriceValidationResult Success() => new() { IsValid = true };

    public static PriceValidationResult Fail(string error) => new()
    {
        IsValid = false,
        Errors = new() { error }
    };

    public static PriceValidationResult Warning(string warning) => new()
    {
        IsValid = true,
        Warnings = new() { warning }
    };
}

/// <summary>
/// Metrics captured during validation
/// </summary>
public class ValidationMetrics
{
    public decimal? DeviationPercent { get; set; }
    public TimeSpan? DataAge { get; set; }
    public decimal? VolumeRatio { get; set; }
    public bool IsOutlier { get; set; }
}
