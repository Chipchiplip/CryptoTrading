using System;

namespace CryptoTrading.Models.Market;

/// <summary>
/// Represents a market quote with Bid, Ask, and Mid prices
/// </summary>
public class MarketQuote
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Best bid price (price buyers are willing to pay)
    /// </summary>
    public decimal Bid { get; set; }

    /// <summary>
    /// Best ask price (price sellers are asking for)
    /// </summary>
    public decimal Ask { get; set; }

    /// <summary>
    /// Mid price: (Bid + Ask) / 2 - Used for bot calculations, risk, and PnL
    /// </summary>
    public decimal Mid => (Bid + Ask) / 2m;

    /// <summary>
    /// Last traded price (spot price from upstream source)
    /// </summary>
    public decimal Last { get; set; }

    /// <summary>
    /// Quote timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Data source (e.g., "CoinGecko", "Mock")
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Spread percentage: (Ask - Bid) / Mid * 100
    /// </summary>
    public decimal SpreadPercent => Mid > 0 ? ((Ask - Bid) / Mid) * 100 : 0;
}
