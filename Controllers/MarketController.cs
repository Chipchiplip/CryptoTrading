using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Services.MarketData;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;

    public MarketController(IMarketDataService marketDataService)
    {
        _marketDataService = marketDataService;
    }

    /// <summary>
    /// Get all cryptocurrencies
    /// </summary>
    [HttpGet("cryptocurrencies")]
    public async Task<IActionResult> GetCryptocurrencies()
    {
        var result = await _marketDataService.GetCryptocurrenciesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get cryptocurrency by symbol
    /// </summary>
    [HttpGet("cryptocurrencies/{symbol}")]
    public async Task<IActionResult> GetCryptocurrency(string symbol)
    {
        var result = await _marketDataService.GetCryptocurrencyAsync(symbol);
        return Ok(result);
    }

    /// <summary>
    /// Get current prices
    /// </summary>
    [HttpGet("prices")]
    public async Task<IActionResult> GetPrices([FromQuery] string[] symbols)
    {
        var result = await _marketDataService.GetPricesAsync(symbols);
        return Ok(result);
    }

    /// <summary>
    /// Get market statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetMarketStats()
    {
        var result = await _marketDataService.GetMarketStatsAsync();
        return Ok(result);
    }
}
