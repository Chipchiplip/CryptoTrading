using CryptoTradingApp.Models.Market;
using Microsoft.Extensions.Logging;

namespace CryptoTradingApp.Services.Market;

/// <summary>
/// Mock exchange data provider for testing and development
/// </summary>
public class MockExchangeDataProvider : IExchangeDataProvider
{
    private readonly ILogger<MockExchangeDataProvider> _logger;
    private readonly Random _random = new();
    private readonly Dictionary<string, decimal> _basePrices = new()
    {
        ["BTCUSDT"] = 50000m,
        ["ETHUSDT"] = 3000m,
        ["BNBUSDT"] = 400m,
        ["ADAUSDT"] = 0.50m,
        ["SOLUSDT"] = 100m
    };

    public string ExchangeName => "Mock";

    public MockExchangeDataProvider(ILogger<MockExchangeDataProvider> logger)
    {
        _logger = logger;
    }

    public Task<decimal?> GetMarkPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var basePrice = GetBasePrice(symbol);
        // Add small random variation (±0.5%)
        var variation = (decimal)(_random.NextDouble() * 0.01 - 0.005);
        var price = basePrice * (1 + variation);

        _logger.LogDebug("Mock mark price for {Symbol}: {Price}", symbol, price);

        return Task.FromResult<decimal?>(price);
    }

    public async Task<decimal?> GetOrderBookMidPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var orderBook = await GetOrderBookAsync(symbol, 1, cancellationToken);
        return orderBook?.GetMidPrice();
    }

    public Task<OrderBook?> GetOrderBookAsync(
        string symbol,
        int depth = 20,
        CancellationToken cancellationToken = default)
    {
        var basePrice = GetBasePrice(symbol);
        var spreadPercent = 0.05m; // 0.05% spread

        var orderBook = new OrderBook
        {
            Symbol = symbol,
            Timestamp = DateTime.UtcNow,
            Bids = new List<OrderBookLevel>(),
            Asks = new List<OrderBookLevel>()
        };

        // Generate bid levels (below mid price)
        for (int i = 0; i < depth; i++)
        {
            var priceOffset = (spreadPercent / 2 + i * 0.01m) / 100;
            var price = basePrice * (1 - priceOffset);
            var quantity = (decimal)_random.NextDouble() * 10 + 1;

            orderBook.Bids.Add(new OrderBookLevel
            {
                Price = Math.Round(price, 2),
                Quantity = Math.Round(quantity, 4)
            });
        }

        // Generate ask levels (above mid price)
        for (int i = 0; i < depth; i++)
        {
            var priceOffset = (spreadPercent / 2 + i * 0.01m) / 100;
            var price = basePrice * (1 + priceOffset);
            var quantity = (decimal)_random.NextDouble() * 10 + 1;

            orderBook.Asks.Add(new OrderBookLevel
            {
                Price = Math.Round(price, 2),
                Quantity = Math.Round(quantity, 4)
            });
        }

        _logger.LogDebug(
            "Mock order book for {Symbol}: Mid={Mid}, Spread={Spread:F4}%",
            symbol, orderBook.GetMidPrice(), orderBook.GetSpreadPercent());

        return Task.FromResult<OrderBook?>(orderBook);
    }

    public async Task<TickerData?> GetTickerAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var price = await GetMarkPriceAsync(symbol, cancellationToken) ?? 0;
        var change24h = (decimal)(_random.NextDouble() * 10 - 5); // ±5%

        var ticker = new TickerData
        {
            Symbol = symbol,
            LastPrice = price,
            BidPrice = price * 0.9995m,
            AskPrice = price * 1.0005m,
            Volume24h = (decimal)_random.Next(1000000, 10000000),
            PriceChange24h = price * (change24h / 100),
            PriceChangePercent24h = change24h,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogDebug(
            "Mock ticker for {Symbol}: Price={Price}, Change={Change:F2}%",
            symbol, ticker.LastPrice, ticker.PriceChangePercent24h);

        return ticker;
    }

    public Task<List<OHLCVCandle>> GetCandlesAsync(
        string symbol,
        string interval,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var basePrice = GetBasePrice(symbol);
        var candles = new List<OHLCVCandle>();

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

        var currentTime = DateTime.UtcNow;
        var currentPrice = basePrice;

        // Generate candles going backwards in time
        for (int i = limit - 1; i >= 0; i--)
        {
            var candleTime = currentTime.AddMinutes(-i * intervalMinutes);

            // Random walk with trend
            var change = (decimal)(_random.NextDouble() * 0.02 - 0.01); // ±1%
            currentPrice = currentPrice * (1 + change);

            var high = currentPrice * (1 + (decimal)_random.NextDouble() * 0.005m);
            var low = currentPrice * (1 - (decimal)_random.NextDouble() * 0.005m);
            var open = low + (high - low) * (decimal)_random.NextDouble();
            var close = low + (high - low) * (decimal)_random.NextDouble();

            candles.Add(new OHLCVCandle
            {
                Timestamp = candleTime,
                Open = Math.Round(open, 2),
                High = Math.Round(high, 2),
                Low = Math.Round(low, 2),
                Close = Math.Round(close, 2),
                Volume = (decimal)_random.Next(100000, 1000000)
            });
        }

        _logger.LogDebug("Generated {Count} mock candles for {Symbol}", candles.Count, symbol);

        return Task.FromResult(candles);
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mock exchange health check: OK");
        return Task.FromResult(true);
    }

    private decimal GetBasePrice(string symbol)
    {
        var normalizedSymbol = symbol.Replace("-", "").Replace("/", "").ToUpperInvariant();

        if (_basePrices.TryGetValue(normalizedSymbol, out var price))
            return price;

        // Default fallback price
        return 1000m;
    }
}
