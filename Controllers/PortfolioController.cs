using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Services.Portfolio;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfolioController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    /// <summary>
    /// Get user watchlists
    /// </summary>
    [HttpGet("watchlists")]
    public async Task<IActionResult> GetWatchlists()
    {
        var result = await _portfolioService.GetWatchlistsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Create new watchlist
    /// </summary>
    [HttpPost("watchlists")]
    public async Task<IActionResult> CreateWatchlist([FromBody] CreateWatchlistDto dto)
    {
        var result = await _portfolioService.CreateWatchlistAsync(dto);
        return CreatedAtAction(nameof(GetWatchlist), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get watchlist by ID
    /// </summary>
    [HttpGet("watchlists/{id}")]
    public async Task<IActionResult> GetWatchlist(Guid id)
    {
        var result = await _portfolioService.GetWatchlistAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Get portfolio overview
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetPortfolioOverview()
    {
        var result = await _portfolioService.GetPortfolioOverviewAsync();
        return Ok(result);
    }
}

// DTOs
public record CreateWatchlistDto(string Name, bool IsDefault = false);
public record WatchlistDto(Guid Id, string Name, bool IsDefault, DateTime CreatedAtUtc);
public record PortfolioOverviewDto(decimal TotalValue, decimal TotalCost, decimal UnrealizedPnL);
