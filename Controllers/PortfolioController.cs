using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    /// <summary>
    /// Get user watchlists
    /// </summary>
    [HttpGet("watchlists")]
    public async Task<IActionResult> GetWatchlists()
    {
        // TODO: Implement portfolio service
        return Ok(new { message = "Get watchlists endpoint - to be implemented by team member" });
    }

    /// <summary>
    /// Create new watchlist
    /// </summary>
    [HttpPost("watchlists")]
    public async Task<IActionResult> CreateWatchlist([FromBody] CreateWatchlistDto dto)
    {
        // TODO: Implement portfolio service
        return Ok(new { message = "Create watchlist endpoint - to be implemented by team member", watchlistName = dto.Name });
    }

    /// <summary>
    /// Get watchlist by ID
    /// </summary>
    [HttpGet("watchlists/{id}")]
    public async Task<IActionResult> GetWatchlist(Guid id)
    {
        // TODO: Implement portfolio service
        return Ok(new { message = $"Get watchlist {id} endpoint - to be implemented by team member" });
    }

    /// <summary>
    /// Get portfolio overview
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetPortfolioOverview()
    {
        // TODO: Implement portfolio service
        return Ok(new { message = "Get portfolio overview endpoint - to be implemented by team member" });
    }
}

// DTOs
public record CreateWatchlistDto(string Name, bool IsDefault = false);
