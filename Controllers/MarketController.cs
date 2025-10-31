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
}
