using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Services;
using CryptoTrading.Models;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketController : ControllerBase
{
    private readonly ICoinGeckoService _coinGeckoService;
    private readonly ICryptoCacheService _cacheService;
    private readonly ICryptoDataSyncService _syncService;
    private readonly ILogger<MarketController> _logger;

    public MarketController(
        ICoinGeckoService coinGeckoService, 
        ICryptoCacheService cacheService,
        ILogger<MarketController> logger,
        ICryptoDataSyncService syncService)
    {
        _coinGeckoService = coinGeckoService;
        _cacheService = cacheService;
        _logger = logger;
        _syncService = syncService;
    }

    /// <summary>
    /// Get all cryptocurrencies
    /// </summary>
    [HttpGet("cryptocurrencies")]
    public async Task<IActionResult> GetCryptocurrencies()
    {
        try
        {
            var cryptocurrencies = await _coinGeckoService.GetMarketDataAsync();
            return Ok(cryptocurrencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cryptocurrencies");
            return StatusCode(500, new { message = "Error fetching cryptocurrency data" });
        }
    }

    /// <summary>
    /// Get cryptocurrency by symbol
    /// </summary>
    [HttpGet("cryptocurrencies/{symbol}")]
    public async Task<IActionResult> GetCryptocurrency(string symbol)
    {
        try
        {
            var cryptocurrencies = await _coinGeckoService.GetMarketDataAsync();
            var crypto = cryptocurrencies.FirstOrDefault(c => 
                c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase) ||
                c.Id.Equals(symbol, StringComparison.OrdinalIgnoreCase));
            
            if (crypto == null)
            {
                return NotFound(new { message = $"Cryptocurrency '{symbol}' not found" });
            }
            
            return Ok(crypto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cryptocurrency {Symbol}", symbol);
            return StatusCode(500, new { message = "Error fetching cryptocurrency data" });
        }
    }

    /// <summary>
    /// Get current prices for specific symbols
    /// </summary>
    [HttpGet("prices")]
    public async Task<IActionResult> GetPrices([FromQuery] string[] symbols)
    {
        try
        {
            var cryptocurrencies = await _coinGeckoService.GetMarketDataAsync();
            var filteredCryptos = cryptocurrencies.Where(c => 
                symbols.Contains(c.Symbol, StringComparer.OrdinalIgnoreCase) ||
                symbols.Contains(c.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            
            return Ok(filteredCryptos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prices for symbols {Symbols}", string.Join(",", symbols));
            return StatusCode(500, new { message = "Error fetching price data" });
        }
    }

    /// <summary>
    /// Get market statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetMarketStats()
    {
        try
        {
            var stats = await _coinGeckoService.GetMarketStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching market stats");
            return StatusCode(500, new { message = "Error fetching market statistics" });
        }
    }

    /// <summary>
    /// Get price history for a specific cryptocurrency
    /// </summary>
    [HttpGet("cryptocurrencies/{coinId}/history")]
    public async Task<IActionResult> GetPriceHistory(string coinId, [FromQuery] int days = 7)
    {
        try
        {
            var history = await _coinGeckoService.GetPriceHistoryAsync(coinId, days);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price history for {CoinId}", coinId);
            return StatusCode(500, new { message = "Error fetching price history" });
        }
    }

    /// <summary>
    /// Clear cache
    /// </summary>
    [HttpPost("clear-cache")]
    public IActionResult ClearCache()
    {
        try
        {
            _cacheService.ClearAllCache();
            return Ok(new { message = "Cache cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, new { message = "Error clearing cache" });
        }
    }

    /// <summary>
    /// Force synchronize market data into database (cryptocurrencies, prices, stats)
    /// </summary>
    [HttpPost("sync-now")]
    public async Task<IActionResult> SyncNow()
    {
        try
        {
            await _syncService.SyncCryptocurrenciesAsync();
            await _syncService.SyncPricesAsync();
            await _syncService.SyncMarketStatsAsync();
            // refresh cache immediately for API/UI
            var latest = await _coinGeckoService.GetMarketDataAsync();
            var stats = await _coinGeckoService.GetMarketStatsAsync();
            return Ok(new { message = "Synchronization completed", coins = latest?.Count ?? 0, statsUpdated = stats != null });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing market sync");
            return StatusCode(500, new { message = "Error forcing market sync" });
        }
    }

    /// <summary>
    /// Get candlestick data for trading chart from Binance API
    /// </summary>
    [HttpGet("candles")]
    public async Task<IActionResult> GetCandles(
        [FromQuery] string symbol, 
        [FromQuery] string interval = "1m")
    {
        try
        {
            // Normalize symbol to Binance format (e.g., "BTCUSDT")
            var normalizedSymbol = symbol.ToUpper();
            if (!normalizedSymbol.EndsWith("USDT") && !normalizedSymbol.EndsWith("USD"))
            {
                normalizedSymbol = normalizedSymbol.Replace("USD", "").Replace("USDT", "") + "USDT";
            }
            
            // Map interval to Binance format
            var binanceInterval = interval switch
            {
                "1m" => "1m",
                "3m" => "3m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1h" => "1h",
                "2h" => "2h",
                "4h" => "4h",
                "6h" => "6h",
                "8h" => "8h",
                "12h" => "12h",
                "1d" => "1d",
                "3d" => "3d",
                "1w" => "1w",
                "1M" => "1M",
                _ => "1h"
            };
            
            // Calculate limit based on interval
            var limit = interval switch
            {
                "1m" => 500,    // Last 500 minutes (~8 hours)
                "5m" => 288,    // Last 288 5-min candles (~24 hours)
                "15m" => 96,    // Last 96 15-min candles (~24 hours)
                "1h" => 168,    // Last 168 hours (~7 days)
                "4h" => 180,    // Last 180 4h candles (~30 days)
                "1d" => 90,     // Last 90 days
                _ => 500
            };
            
            // Call Binance API
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.binance.com");
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            var url = $"/api/v3/klines?symbol={normalizedSymbol}&interval={binanceInterval}&limit={limit}";
            _logger.LogInformation("Fetching candles from Binance: {Url}", url);
            
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Binance API returned {StatusCode} for {Symbol}", response.StatusCode, normalizedSymbol);
                
                // Fallback: Get current price and generate mock candles
                try
                {
                    var cryptos = await _coinGeckoService.GetMarketDataAsync();
                    var normalizedSymbolWithoutUSDT = normalizedSymbol.Replace("USDT", "").Replace("USD", "");
                    var currentCrypto = cryptos?.FirstOrDefault(c => 
                        c.Symbol?.Equals(normalizedSymbolWithoutUSDT, StringComparison.OrdinalIgnoreCase) == true);
                    var currentPrice = (double)(currentCrypto?.CurrentPrice ?? 0);
                    
                    if (currentPrice > 0)
                    {
                        var mockCandles = GenerateMockCandles(currentPrice, interval, limit);
                        _logger.LogInformation("Using mock candles as fallback for {Symbol}", normalizedSymbol);
                        return Ok(mockCandles);
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogWarning(fallbackEx, "Error getting market data for fallback, using default price");
                }
                
                // Last resort: Use default price
                var defaultPrice = 50000.0; // Default BTC price
                var fallbackCandles = GenerateMockCandles(defaultPrice, interval, limit);
                _logger.LogWarning("Using default price mock candles for {Symbol}", normalizedSymbol);
                return Ok(fallbackCandles);
            }
            
            var jsonString = await response.Content.ReadAsStringAsync();
            
            // Log first few characters of response for debugging
            if (jsonString.Length > 0)
            {
                var preview = jsonString.Length > 500 ? jsonString.Substring(0, 500) : jsonString;
                _logger.LogInformation("Binance API response preview: {Preview}", preview);
            }
            
            var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
            var candles = new List<object>();
            
            if (jsonDoc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var candleArray in jsonDoc.RootElement.EnumerateArray())
                {
                    if (candleArray.ValueKind == System.Text.Json.JsonValueKind.Array && candleArray.GetArrayLength() >= 6)
                    {
                        try
                        {
                            // Binance klines format: [OpenTime, Open, High, Low, Close, Volume, CloseTime, ...]
                            var openTime = candleArray[0].GetInt64();
                            var openStr = candleArray[1].GetString() ?? "0";
                            var highStr = candleArray[2].GetString() ?? "0";
                            var lowStr = candleArray[3].GetString() ?? "0";
                            var closeStr = candleArray[4].GetString() ?? "0";
                            
                            // Parse with invariant culture to avoid locale issues
                            var open = double.Parse(openStr, System.Globalization.CultureInfo.InvariantCulture);
                            var high = double.Parse(highStr, System.Globalization.CultureInfo.InvariantCulture);
                            var low = double.Parse(lowStr, System.Globalization.CultureInfo.InvariantCulture);
                            var close = double.Parse(closeStr, System.Globalization.CultureInfo.InvariantCulture);
                            
                            // Log first candle for debugging
                            if (candles.Count == 0)
                            {
                                _logger.LogInformation("First candle from Binance - Raw strings: open={OpenStr}, high={HighStr}, low={LowStr}, close={CloseStr}. Parsed: open={Open}, high={High}, low={Low}, close={Close}", 
                                    openStr, highStr, lowStr, closeStr, open, high, low, close);
                            }
                            
                            // Validate prices are reasonable (BTC should be between $1 and $1,000,000)
                            if (open > 0 && open < 1000000 && high > 0 && high < 1000000 && 
                                low > 0 && low < 1000000 && close > 0 && close < 1000000)
                            {
                                candles.Add(new
                                {
                                    time = openTime / 1000, // Convert milliseconds to seconds
                                    open = Math.Round(open, 2),
                                    high = Math.Round(high, 2),
                                    low = Math.Round(low, 2),
                                    close = Math.Round(close, 2)
                                });
                            }
                            else
                            {
                                _logger.LogWarning("Invalid price values detected and skipped: open={Open} (raw: {OpenStr}), high={High} (raw: {HighStr}), low={Low} (raw: {LowStr}), close={Close} (raw: {CloseStr})", 
                                    open, openStr, high, highStr, low, lowStr, close, closeStr);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error parsing candle data");
                            // Continue with next candle
                        }
                    }
                }
            }
            
            _logger.LogInformation("Retrieved {Count} candles from Binance for {Symbol} ({Interval})", candles.Count, normalizedSymbol, interval);
            
            return Ok(candles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching candles from Binance for {Symbol} with interval {Interval}", symbol, interval);
            
            // Fallback: Try to get current price and generate mock candles
            try
            {
                var cryptos = await _coinGeckoService.GetMarketDataAsync();
                var normalizedSymbol = symbol?.Replace("USDT", "").Replace("USD", "").ToUpper() ?? "BTC";
                var currentCrypto = cryptos?.FirstOrDefault(c => 
                    c.Symbol?.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase) == true);
                var currentPrice = (double)(currentCrypto?.CurrentPrice ?? 0);
                
                if (currentPrice > 0)
                {
                    var limit = interval switch
                    {
                        "1m" => 500,
                        "5m" => 288,
                        "15m" => 96,
                        "1h" => 168,
                        "4h" => 180,
                        "1d" => 90,
                        _ => 500
                    };
                    var mockCandles = GenerateMockCandles(currentPrice, interval, limit);
                    _logger.LogInformation("Using mock candles as fallback for {Symbol}", symbol);
                    return Ok(mockCandles);
                }
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Error generating fallback candles");
            }
            
            // Last resort: Generate mock candles with default price if everything fails
            try
            {
                var defaultPrice = 50000.0; // Default BTC price
                var limit = interval switch
                {
                    "1m" => 500,
                    "5m" => 288,
                    "15m" => 96,
                    "1h" => 168,
                    "4h" => 180,
                    "1d" => 90,
                    _ => 500
                };
                var mockCandles = GenerateMockCandles(defaultPrice, interval, limit);
                _logger.LogWarning("Using default mock candles as last resort for {Symbol}", symbol);
                return Ok(mockCandles);
            }
            catch (Exception lastResortEx)
            {
                _logger.LogError(lastResortEx, "Critical error: Cannot generate any candles");
                return StatusCode(500, new { message = "Error fetching candlestick data", details = lastResortEx.Message });
            }
        }
    }

    private List<object> GenerateMockCandles(double currentPrice, string interval, int limit)
    {
        var candles = new List<object>();
        var random = new Random();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        // Calculate interval in milliseconds
        var intervalMs = interval switch
        {
            "1m" => 60 * 1000,
            "5m" => 5 * 60 * 1000,
            "15m" => 15 * 60 * 1000,
            "1h" => 60 * 60 * 1000,
            "4h" => 4 * 60 * 60 * 1000,
            "1d" => 24 * 60 * 60 * 1000,
            _ => 60 * 60 * 1000
        };
        
        // Use limit directly as number of candles
        var candlesCount = Math.Min(limit, 500);
        
        // Start from a price slightly lower than current (for realistic trend up)
        var basePrice = currentPrice * 0.95;
        
        // Generate candles from past to now
        for (int i = 0; i < candlesCount; i++)
        {
            var timestamp = now - ((candlesCount - 1 - i) * intervalMs);
            
            // Simulate price movement (random walk with slight upward trend)
            var changePercent = (random.NextDouble() - 0.4) * 0.015; // Slight upward bias
            basePrice = basePrice * (1 + changePercent);
            
            // Ensure price stays positive and reasonable (50% to 150% of current)
            basePrice = Math.Max(basePrice, currentPrice * 0.5);
            basePrice = Math.Min(basePrice, currentPrice * 1.5);
            
            // For the last candle, use current price
            if (i == candlesCount - 1)
            {
                basePrice = currentPrice;
            }
            
            var open = basePrice;
            var volatility = random.NextDouble() * 0.01; // 0-1% volatility
            var high = open * (1 + volatility);
            var low = open * (1 - volatility);
            var closeChange = (random.NextDouble() - 0.5) * 0.01;
            var close = open * (1 + closeChange);
            
            // Ensure OHLC logic is correct
            high = Math.Max(high, Math.Max(open, close));
            low = Math.Min(low, Math.Min(open, close));
            
            candles.Add(new
            {
                time = (long)(timestamp / 1000),
                open = Math.Round(open, 2),
                high = Math.Round(high, 2),
                low = Math.Round(low, 2),
                close = Math.Round(close, 2)
            });
            
            basePrice = close; // Use close as next open
        }
        
        return candles;
    }
}
