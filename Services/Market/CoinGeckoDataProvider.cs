using CryptoTradingApp.Models.Market;
using CryptoTradingApp.Services.CoinGecko;
using CryptoTradingApp.Services.Cache;
using Microsoft.Extensions.Logging;

namespace CryptoTradingApp.Services.Market;

/// <summary>
/// Market data provider using CoinGecko API (fallback/development mode)
/// Note: CoinGecko provides aggregate data, not real exchange orderbook
/// </summary>
public class CoinGeckoDataProvider : IExchangeDataProvider
{
    private readonly ILogger<CoinGeckoDataProvider> _logger;
    private readonly ICoinGeckoService _coinGeckoService;
    private readonly ICryptoCacheService _cache;

    public string ExchangeName => "CoinGecko";

    public CoinGeckoDataProvider(
        ILogger<CoinGeckoDataProvider> logger,
        ICoinGeckoService coinGeckoService,
        ICryptoCacheService cache)
    {
        _logger = logger;
        _coinGeckoService = coinGeckoService;
        _cache = cache;
    }

    public async Task<decimal?> GetMarkPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var coinId = ConvertSymbolToCoinId(symbol);
            var marketData = await _coinGeckoService.GetMarketDataAsync(coinId);

            if (marketData?.CurrentPrice != null)
            {
                _logger.LogDebug("CoinGecko price for {Symbol}: {Price}", symbol, marketData.CurrentPrice);
                return marketData.CurrentPrice;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch price from CoinGecko for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<decimal?> GetOrderBookMidPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // CoinGecko doesn't provide real order book data
        // Fall back to market price
        _logger.LogWarning(
            "CoinGecko does not provide order book data, using market price for {Symbol}",
            symbol);

        return await GetMarkPriceAsync(symbol, cancellationToken);
    }

    public async Task<OrderBook?> GetOrderBookAsync(
        string symbol,
        int depth = 20,
        CancellationToken cancellationToken = default)
    {
        // CoinGecko doesn't provide order book
        _logger.LogWarning("CoinGecko does not provide order book data for {Symbol}", symbol);

        // Return a synthetic orderbook with just the current price
        var price = await GetMarkPriceAsync(symbol, cancellationToken);
        if (price == null)
            return null;

        // Create synthetic spread (±0.05%)
        var spreadPercent = 0.05m;
        var bidPrice = price.Value * (1 - spreadPercent / 100);
        var askPrice = price.Value * (1 + spreadPercent / 100);

        return new OrderBook
        {
            Symbol = symbol,
            Timestamp = DateTime.UtcNow,
            Bids = new List<OrderBookLevel>
            {
                new() { Price = bidPrice, Quantity = 1.0m }
            },
            Asks = new List<OrderBookLevel>
            {
                new() { Price = askPrice, Quantity = 1.0m }
            }
        };
    }

    public async Task<TickerData?> GetTickerAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var coinId = ConvertSymbolToCoinId(symbol);
            var marketData = await _coinGeckoService.GetMarketDataAsync(coinId);

            if (marketData == null)
                return null;

            // Synthetic bid/ask with ±0.05% spread
            var spreadPercent = 0.05m;
            var midPrice = marketData.CurrentPrice ?? 0;
            var bidPrice = midPrice * (1 - spreadPercent / 100);
            var askPrice = midPrice * (1 + spreadPercent / 100);

            return new TickerData
            {
                Symbol = symbol,
                LastPrice = midPrice,
                BidPrice = bidPrice,
                AskPrice = askPrice,
                Volume24h = marketData.TotalVolume ?? 0,
                PriceChange24h = marketData.PriceChange24h ?? 0,
                PriceChangePercent24h = marketData.PriceChangePercentage24h ?? 0,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch ticker from CoinGecko for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<List<OHLCVCandle>> GetCandlesAsync(
        string symbol,
        string interval,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var coinId = ConvertSymbolToCoinId(symbol);
            var days = CalculateDaysFromInterval(interval, limit);

            var priceHistory = await _coinGeckoService.GetPriceHistoryAsync(coinId, days);

            if (priceHistory == null || priceHistory.Count == 0)
                return new List<OHLCVCandle>();

            // CoinGecko only provides price points, we need to aggregate into candles
            var candles = AggregateIntoCandles(priceHistory, interval, limit);

            _logger.LogDebug("Generated {Count} candles from CoinGecko for {Symbol}", candles.Count, symbol);

            return candles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch candles from CoinGecko for {Symbol}", symbol);
            return new List<OHLCVCandle>();
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to fetch Bitcoin price as health check
            var marketData = await _coinGeckoService.GetMarketDataAsync("bitcoin");
            var isHealthy = marketData?.CurrentPrice != null;

            _logger.LogDebug("CoinGecko health check: {Status}", isHealthy ? "OK" : "FAILED");

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CoinGecko health check failed");
            return false;
        }
    }

    private static string ConvertSymbolToCoinId(string symbol)
    {
        // Convert trading symbols to CoinGecko IDs
        var normalized = symbol.Replace("-", "").Replace("/", "").ToUpperInvariant();

        // Remove common quote currencies
        normalized = normalized.Replace("USDT", "").Replace("USD", "").Replace("BUSD", "");

        return normalized.ToLowerInvariant() switch
        {
            "BTC" => "bitcoin",
            "ETH" => "ethereum",
            "BNB" => "binancecoin",
            "ADA" => "cardano",
            "SOL" => "solana",
            "XRP" => "ripple",
            "DOT" => "polkadot",
            "DOGE" => "dogecoin",
            "AVAX" => "avalanche-2",
            "MATIC" => "matic-network",
            _ => normalized.ToLowerInvariant()
        };
    }

    private static int CalculateDaysFromInterval(string interval, int limit)
    {
        var intervalMinutes = interval.ToLowerInvariant() switch
        {
            "1m" or "1min" => 1,
            "5m" or "5min" => 5,
            "15m" or "15min" => 15,
            "1h" or "1hour" => 60,
            "4h" or "4hour" => 240,
            "1d" or "1day" => 1440,
            _ => 60 // default to 1 hour
        };

        var totalMinutes = intervalMinutes * limit;
        var days = Math.Max(1, (int)Math.Ceiling(totalMinutes / 1440.0));

        // CoinGecko limits: max 90 days
        return Math.Min(days, 90);
    }

    private static List<OHLCVCandle> AggregateIntoCandles(
        List<PriceHistoryPoint> priceHistory,
        string interval,
        int limit)
    {
        if (priceHistory.Count == 0)
            return new List<OHLCVCandle>();

        var intervalMinutes = interval.ToLowerInvariant() switch
        {
            "1m" or "1min" => 1,
            "5m" or "5min" => 5,
            "15m" or "15min" => 15,
            "1h" or "1hour" => 60,
            "4h" or "4hour" => 240,
            "1d" or "1day" => 1440,
            _ => 60
        };

        // Group price points into candle intervals
        var candles = new List<OHLCVCandle>();
        var sortedHistory = priceHistory.OrderBy(p => p.Timestamp).ToList();

        var currentCandleStart = sortedHistory.First().Timestamp;
        var currentCandlePrices = new List<decimal>();

        foreach (var point in sortedHistory)
        {
            var timeDiff = (point.Timestamp - currentCandleStart).TotalMinutes;

            if (timeDiff >= intervalMinutes)
            {
                // Create candle from accumulated prices
                if (currentCandlePrices.Count > 0)
                {
                    candles.Add(CreateCandle(currentCandleStart, currentCandlePrices));
                }

                // Start new candle
                currentCandleStart = point.Timestamp;
                currentCandlePrices = new List<decimal> { point.Price };
            }
            else
            {
                currentCandlePrices.Add(point.Price);
            }
        }

        // Add final candle
        if (currentCandlePrices.Count > 0)
        {
            candles.Add(CreateCandle(currentCandleStart, currentCandlePrices));
        }

        // Return only the requested limit
        return candles.TakeLast(limit).ToList();
    }

    private static OHLCVCandle CreateCandle(DateTime timestamp, List<decimal> prices)
    {
        return new OHLCVCandle
        {
            Timestamp = timestamp,
            Open = prices.First(),
            High = prices.Max(),
            Low = prices.Min(),
            Close = prices.Last(),
            Volume = 0 // CoinGecko doesn't provide granular volume data
        };
    }
}

public class PriceHistoryPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Price { get; set; }
}
