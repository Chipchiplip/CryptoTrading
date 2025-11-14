using CryptoTradingApp.Models.Market;

namespace CryptoTradingApp.Services.Market;

/// <summary>
/// Interface for fetching real-time market data from exchanges
/// </summary>
public interface IExchangeDataProvider
{
    /// <summary>
    /// Exchange name (e.g., "Binance", "Coinbase", "CoinGecko")
    /// </summary>
    string ExchangeName { get; }

    /// <summary>
    /// Gets the current mark price (index price used for funding)
    /// </summary>
    Task<decimal?> GetMarkPriceAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the order book mid price: (best_bid + best_ask) / 2
    /// </summary>
    Task<decimal?> GetOrderBookMidPriceAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full order book with specified depth
    /// </summary>
    Task<OrderBook?> GetOrderBookAsync(string symbol, int depth = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest ticker data
    /// </summary>
    Task<TickerData?> GetTickerAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets OHLCV candles for the specified interval and limit
    /// </summary>
    Task<List<OHLCVCandle>> GetCandlesAsync(
        string symbol,
        string interval,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the exchange API is healthy and responsive
    /// </summary>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Order book data
/// </summary>
public class OrderBook
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<OrderBookLevel> Bids { get; set; } = new();
    public List<OrderBookLevel> Asks { get; set; } = new();

    /// <summary>
    /// Calculates mid price: (best bid + best ask) / 2
    /// </summary>
    public decimal GetMidPrice()
    {
        var bestBid = Bids.FirstOrDefault()?.Price ?? 0;
        var bestAsk = Asks.FirstOrDefault()?.Price ?? 0;

        if (bestBid == 0 || bestAsk == 0)
            return Math.Max(bestBid, bestAsk);

        return (bestBid + bestAsk) / 2;
    }

    /// <summary>
    /// Calculates spread in percentage
    /// </summary>
    public decimal GetSpreadPercent()
    {
        var midPrice = GetMidPrice();
        if (midPrice == 0)
            return 0;

        var bestBid = Bids.FirstOrDefault()?.Price ?? 0;
        var bestAsk = Asks.FirstOrDefault()?.Price ?? 0;

        return ((bestAsk - bestBid) / midPrice) * 100;
    }
}

public class OrderBookLevel
{
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
}

/// <summary>
/// Ticker data from exchange
/// </summary>
public class TickerData
{
    public string Symbol { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal BidPrice { get; set; }
    public decimal AskPrice { get; set; }
    public decimal Volume24h { get; set; }
    public decimal PriceChange24h { get; set; }
    public decimal PriceChangePercent24h { get; set; }
    public DateTime Timestamp { get; set; }

    public decimal GetMidPrice() => (BidPrice + AskPrice) / 2;
}
