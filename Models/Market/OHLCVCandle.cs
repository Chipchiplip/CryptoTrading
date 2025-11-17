using System;

namespace CryptoTrading.Models.Market;

/// <summary>
/// Represents an OHLCV (Open, High, Low, Close, Volume) candlestick
/// </summary>
public class OHLCVCandle
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Candle open timestamp
    /// </summary>
    public DateTime OpenTime { get; set; }

    /// <summary>
    /// Opening price
    /// </summary>
    public decimal Open { get; set; }

    /// <summary>
    /// Highest price during the period
    /// </summary>
    public decimal High { get; set; }

    /// <summary>
    /// Lowest price during the period
    /// </summary>
    public decimal Low { get; set; }

    /// <summary>
    /// Closing price
    /// </summary>
    public decimal Close { get; set; }

    /// <summary>
    /// Trading volume
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    /// Candle close timestamp
    /// </summary>
    public DateTime CloseTime { get; set; }

    /// <summary>
    /// Timestamp property for compatibility (returns OpenTime)
    /// </summary>
    public DateTime Timestamp
    {
        get => OpenTime;
        set => OpenTime = value;
    }

    /// <summary>
    /// Quote asset volume (e.g., USDT volume for BTCUSDT)
    /// </summary>
    public decimal QuoteVolume { get; set; }

    /// <summary>
    /// Number of trades during the period
    /// </summary>
    public int NumberOfTrades { get; set; }
}
