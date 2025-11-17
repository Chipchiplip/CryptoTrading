using CryptoTrading.Models.Market;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

namespace CryptoTrading.Services.Market;

/// <summary>
/// Real-time market data provider for Binance exchange
/// </summary>
public class BinanceDataProvider : IExchangeDataProvider
{
    private readonly ILogger<BinanceDataProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;

    private const string DEFAULT_BINANCE_API = "https://api.binance.com";
    private const int REQUEST_TIMEOUT_SECONDS = 10;

    public string ExchangeName => "Binance";

    public BinanceDataProvider(
        ILogger<BinanceDataProvider> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _baseUrl = configuration["Trading:BinanceApiUrl"] ?? DEFAULT_BINANCE_API;
    }

    public async Task<decimal?> GetMarkPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedSymbol = NormalizeSymbol(symbol);
            var url = $"{_baseUrl}/api/v3/ticker/price?symbol={normalizedSymbol}";

            using var client = CreateHttpClient();
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Binance API returned {StatusCode} for mark price {Symbol}",
                    response.StatusCode, symbol);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<BinancePriceResponse>(json);

            if (data?.Price != null)
            {
                _logger.LogDebug("Binance mark price for {Symbol}: {Price}", symbol, data.Price);
                return decimal.Parse(data.Price);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch mark price from Binance for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<decimal?> GetOrderBookMidPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var orderBook = await GetOrderBookAsync(symbol, 1, cancellationToken);
            if (orderBook == null)
                return null;

            return orderBook.GetMidPrice();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch order book mid price from Binance for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<OrderBook?> GetOrderBookAsync(
        string symbol,
        int depth = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedSymbol = NormalizeSymbol(symbol);
            var limit = Math.Min(depth, 5000); // Binance max is 5000
            var url = $"{_baseUrl}/api/v3/depth?symbol={normalizedSymbol}&limit={limit}";

            using var client = CreateHttpClient();
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Binance API returned {StatusCode} for order book {Symbol}",
                    response.StatusCode, symbol);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<BinanceOrderBookResponse>(json);

            if (data == null)
                return null;

            var orderBook = new OrderBook
            {
                Symbol = symbol,
                Timestamp = DateTime.UtcNow,
                Bids = data.Bids.Select(b => new OrderBookLevel
                {
                    Price = decimal.Parse(b[0]),
                    Quantity = decimal.Parse(b[1])
                }).ToList(),
                Asks = data.Asks.Select(a => new OrderBookLevel
                {
                    Price = decimal.Parse(a[0]),
                    Quantity = decimal.Parse(a[1])
                }).ToList()
            };

            _logger.LogDebug(
                "Binance order book for {Symbol}: Mid={Mid}, Spread={Spread:F4}%",
                symbol, orderBook.GetMidPrice(), orderBook.GetSpreadPercent());

            return orderBook;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch order book from Binance for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<MarketQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get order book and use best bid/ask to create quote
            var orderBook = await GetOrderBookAsync(symbol, 1, cancellationToken);
            if (orderBook == null || orderBook.Bids.Count == 0 || orderBook.Asks.Count == 0)
            {
                // Fallback to ticker
                var ticker = await GetTickerAsync(symbol, cancellationToken);
                if (ticker == null)
                    return null;

                return new MarketQuote
                {
                    Symbol = symbol,
                    Bid = ticker.BidPrice,
                    Ask = ticker.AskPrice,
                    Last = ticker.LastPrice,
                    Timestamp = ticker.Timestamp,
                    Source = "Binance"
                };
            }

            var bestBid = orderBook.Bids.First().Price;
            var bestAsk = orderBook.Asks.First().Price;

            return new MarketQuote
            {
                Symbol = symbol,
                Bid = bestBid,
                Ask = bestAsk,
                Last = (bestBid + bestAsk) / 2,
                Timestamp = orderBook.Timestamp,
                Source = "Binance"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quote from Binance for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<TickerData?> GetTickerAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedSymbol = NormalizeSymbol(symbol);
            var url = $"{_baseUrl}/api/v3/ticker/24hr?symbol={normalizedSymbol}";

            using var client = CreateHttpClient();
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Binance API returned {StatusCode} for ticker {Symbol}",
                    response.StatusCode, symbol);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<BinanceTickerResponse>(json);

            if (data == null)
                return null;

            var ticker = new TickerData
            {
                Symbol = symbol,
                LastPrice = decimal.Parse(data.LastPrice),
                BidPrice = decimal.Parse(data.BidPrice),
                AskPrice = decimal.Parse(data.AskPrice),
                Volume24h = decimal.Parse(data.Volume),
                PriceChange24h = decimal.Parse(data.PriceChange),
                PriceChangePercent24h = decimal.Parse(data.PriceChangePercent),
                Timestamp = DateTime.UtcNow
            };

            _logger.LogDebug(
                "Binance ticker for {Symbol}: Last={Last}, Change={Change:F2}%",
                symbol, ticker.LastPrice, ticker.PriceChangePercent24h);

            return ticker;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch ticker from Binance for {Symbol}", symbol);
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
            var normalizedSymbol = NormalizeSymbol(symbol);
            var binanceInterval = ConvertInterval(interval);
            var url = $"{_baseUrl}/api/v3/klines?symbol={normalizedSymbol}&interval={binanceInterval}&limit={Math.Min(limit, 1000)}";

            using var client = CreateHttpClient();
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Binance API returned {StatusCode} for candles {Symbol}",
                    response.StatusCode, symbol);
                return new List<OHLCVCandle>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<List<List<JsonElement>>>(json);

            if (data == null)
                return new List<OHLCVCandle>();

            var candles = data.Select(k => new OHLCVCandle
            {
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(k[0].GetInt64()).UtcDateTime,
                Open = decimal.Parse(k[1].GetString()!),
                High = decimal.Parse(k[2].GetString()!),
                Low = decimal.Parse(k[3].GetString()!),
                Close = decimal.Parse(k[4].GetString()!),
                Volume = decimal.Parse(k[5].GetString()!)
            }).ToList();

            _logger.LogDebug("Fetched {Count} candles from Binance for {Symbol}", candles.Count, symbol);

            return candles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch candles from Binance for {Symbol}", symbol);
            return new List<OHLCVCandle>();
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/v3/ping";

            using var client = CreateHttpClient();
            var response = await client.GetAsync(url, cancellationToken);

            var isHealthy = response.IsSuccessStatusCode;

            _logger.LogDebug("Binance health check: {Status}", isHealthy ? "OK" : "FAILED");

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binance health check failed");
            return false;
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient("BinanceApi");
        client.Timeout = TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS);
        return client;
    }

    private static string NormalizeSymbol(string symbol)
    {
        // Convert "BTC-USDT" or "BTCUSDT" to "BTCUSDT" (Binance format)
        return symbol.Replace("-", "").Replace("/", "").ToUpperInvariant();
    }

    private static string ConvertInterval(string interval)
    {
        // Convert common interval formats to Binance format
        return interval.ToLowerInvariant() switch
        {
            "1m" or "1min" => "1m",
            "5m" or "5min" => "5m",
            "15m" or "15min" => "15m",
            "1h" or "1hour" => "1h",
            "4h" or "4hour" => "4h",
            "1d" or "1day" => "1d",
            _ => interval
        };
    }

    #region Response DTOs

    private class BinancePriceResponse
    {
        public string Symbol { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
    }

    private class BinanceOrderBookResponse
    {
        public List<List<string>> Bids { get; set; } = new();
        public List<List<string>> Asks { get; set; } = new();
    }

    private class BinanceTickerResponse
    {
        public string Symbol { get; set; } = string.Empty;
        public string LastPrice { get; set; } = string.Empty;
        public string BidPrice { get; set; } = string.Empty;
        public string AskPrice { get; set; } = string.Empty;
        public string Volume { get; set; } = string.Empty;
        public string PriceChange { get; set; } = string.Empty;
        public string PriceChangePercent { get; set; } = string.Empty;
    }

    #endregion
}
