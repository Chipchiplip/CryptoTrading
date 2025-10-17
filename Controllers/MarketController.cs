using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketController : ControllerBase
{
    /// <summary>
    /// Get all cryptocurrencies
    /// </summary>
    [HttpGet("cryptocurrencies")]
    public async Task<IActionResult> GetCryptocurrencies()
    {
        // TODO: Implement market data service
        return Ok(new { message = "Get cryptocurrencies endpoint - to be implemented by team member" });
    }

    /// <summary>
    /// Get cryptocurrency by symbol
    /// </summary>
    [HttpGet("cryptocurrencies/{symbol}")]
    public async Task<IActionResult> GetCryptocurrency(string symbol)
    {
        // TODO: Implement market data service
        return Ok(new { message = $"Get {symbol} cryptocurrency endpoint - to be implemented by team member" });
    }

    /// <summary>
    /// Get current prices
    /// </summary>
    [HttpGet("prices")]
    public async Task<IActionResult> GetPrices([FromQuery] string[] symbols)
    {
        // TODO: Implement market data service
        return Ok(new { message = "Get prices endpoint - to be implemented by team member", symbols });
    }

    /// <summary>
    /// Get market statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetMarketStats()
    {
        // TODO: Implement market data service
        return Ok(new { message = "Get market stats endpoint - to be implemented by team member" });
    }
}
