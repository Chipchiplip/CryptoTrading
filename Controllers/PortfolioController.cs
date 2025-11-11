using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Interfaces;
using CryptoTrading.Models.DTOs;
using System.Security.Claims;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly IWatchlistService _watchlistService;
    private readonly IPortfolioService _portfolioService;

    public PortfolioController(
        IWatchlistService watchlistService,
        IPortfolioService portfolioService)
    {
        _watchlistService = watchlistService;
        _portfolioService = portfolioService;
    }

    private int GetUserId() 
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            throw new UnauthorizedAccessException("Invalid user ID");
        return userId;
    }

    /// <summary>
    /// Get portfolio info
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetPortfolioInfo()
    {
        return Ok(new { 
            message = "Portfolio API - Watchlist features implemented", 
            version = "1.0",
            endpoints = new[] { 
                "GET /api/portfolio - This info (public)",
                "POST /api/auth/register - Register new user (public)",
                "POST /api/auth/login - Login and get JWT token (public)",
                "GET /api/portfolio/watchlists - Get all watchlists (requires auth)",
                "POST /api/portfolio/watchlists - Create watchlist (requires auth)",
                "GET /api/portfolio/watchlists/{id} - Get watchlist details (requires auth)",
                "GET /api/portfolio/watchlists/default - Get default watchlist (requires auth)",
                "PUT /api/portfolio/watchlists/{id}/rename - Rename watchlist (requires auth)",
                "DELETE /api/portfolio/watchlists/{id} - Delete watchlist (requires auth)",
                "POST /api/portfolio/watchlists/default/coins - Add coin to default (requires auth)",
                "POST /api/portfolio/watchlists/{id}/coins - Add coin to watchlist (requires auth)",
                "DELETE /api/portfolio/watchlists/{id}/coins/{symbol} - Remove coin (requires auth)",
                "GET /api/portfolio/watchlists/{id}/realtime - Get realtime prices (requires auth)",
                "GET /api/portfolio/watchlists/quota - Check quota (requires auth)"
            },
            instructions = "First register/login at /api/auth/register or /api/auth/login, then use the JWT token in Authorization header"
        });
    }

    #region Watchlist Management (Use Cases 36, 40, 41, 42)

    /// <summary>
    /// Use Case 40: View all watchlists
    /// </summary>
    [HttpGet("watchlists")]
    public async Task<IActionResult> GetWatchlists()
    {
        try
        {
            var userId = GetUserId();
            var watchlists = await _watchlistService.GetAllWatchlistsAsync(userId);
            return Ok(watchlists);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Use Case 36: Create new watchlist
    /// </summary>
    [HttpPost("watchlists")]
    public async Task<IActionResult> CreateWatchlist([FromBody] CreateWatchlistDto dto)
    {
        try
        {
            var userId = GetUserId();
            
            // Check quota first
            var quota = await _watchlistService.GetWatchlistQuotaAsync(userId);
            if (!quota.CanCreateMore)
            {
                return BadRequest(new { 
                    message = $"Watchlist limit reached. Your {quota.SubscriptionTier} plan allows {quota.MaxAllowed} watchlists." 
                });
            }

            var watchlist = await _watchlistService.CreateWatchlistAsync(userId, dto);
            return CreatedAtAction(nameof(GetWatchlist), new { id = watchlist.Id }, watchlist);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get watchlist by ID with coins
    /// </summary>
    [HttpGet("watchlists/{id}")]
    public async Task<IActionResult> GetWatchlist(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var watchlist = await _watchlistService.GetWatchlistAsync(userId, id);
            return Ok(watchlist);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Use Case 45: Get default watchlist
    /// </summary>
    [HttpGet("watchlists/default")]
    public async Task<IActionResult> GetDefaultWatchlist()
    {
        try
        {
            var userId = GetUserId();
            var watchlist = await _watchlistService.GetDefaultWatchlistAsync(userId);
            return Ok(watchlist);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Use Case 41: Rename watchlist
    /// </summary>
    [HttpPut("watchlists/{id}/rename")]
    public async Task<IActionResult> RenameWatchlist(Guid id, [FromBody] RenameWatchlistDto dto)
    {
        try
        {
            var userId = GetUserId();
            var watchlist = await _watchlistService.RenameWatchlistAsync(userId, id, dto);
            return Ok(watchlist);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Use Case 42: Delete watchlist
    /// </summary>
    [HttpDelete("watchlists/{id}")]
    public async Task<IActionResult> DeleteWatchlist(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _watchlistService.DeleteWatchlistAsync(userId, id);
            
            if (result)
                return NoContent();
            else
                return NotFound(new { message = "Watchlist not found or cannot be deleted" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Coin Management (Use Cases 37, 38, 39)

    /// <summary>
    /// Use Case 37: Add coin to default watchlist
    /// </summary>
    [HttpPost("watchlists/default/coins")]
    public async Task<IActionResult> AddCoinToDefaultWatchlist([FromBody] AddCoinToWatchlistDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _watchlistService.AddCoinToDefaultWatchlistAsync(userId, dto.CoinSymbol);
            
            if (result)
                return Ok(new { message = $"Added {dto.CoinSymbol} to default watchlist" });
            else
                return BadRequest(new { message = "Failed to add coin to default watchlist" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Use Case 38: Add coin to specific watchlist
    /// </summary>
    [HttpPost("watchlists/{id}/coins")]
    public async Task<IActionResult> AddCoinToWatchlist(Guid id, [FromBody] AddCoinToWatchlistDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _watchlistService.AddCoinToWatchlistAsync(userId, id, dto.CoinSymbol);
            
            if (result)
                return Ok(new { message = $"Added {dto.CoinSymbol} to watchlist" });
            else
                return BadRequest(new { message = "Failed to add coin to watchlist" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Use Case 39: Remove coin from watchlist
    /// </summary>
    [HttpDelete("watchlists/{id}/coins/{symbol}")]
    public async Task<IActionResult> RemoveCoinFromWatchlist(Guid id, string symbol)
    {
        try
        {
            var userId = GetUserId();
            var result = await _watchlistService.RemoveCoinFromWatchlistAsync(userId, id, symbol);
            
            if (result)
                return Ok(new { message = $"Removed {symbol} from watchlist" });
            else
                return NotFound(new { message = "Coin not found in watchlist" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Realtime & Quota (Use Cases 43, 44)

    /// <summary>
    /// Use Case 43: Get realtime updates for watchlist
    /// </summary>
    [HttpGet("watchlists/{id}/realtime")]
    public async Task<IActionResult> GetWatchlistRealtimeUpdates(Guid id)
    {
        try
        {
            var updates = await _watchlistService.GetWatchlistRealtimeUpdatesAsync(id);
            return Ok(updates);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Use Case 44: Check watchlist quota
    /// </summary>
    [HttpGet("watchlists/quota")]
    public async Task<IActionResult> GetWatchlistQuota()
    {
        try
        {
            var userId = GetUserId();
            var quota = await _watchlistService.GetWatchlistQuotaAsync(userId);
            return Ok(quota);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Portfolio Overview

    /// <summary>
    /// Get portfolio overview including holdings, PnL, and NAV history
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetPortfolioOverview()
    {
        try
        {
            var userId = GetUserId();
            var overview = await _portfolioService.GetPortfolioOverviewAsync(userId);
            return Ok(overview);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get portfolio performance metrics for a date range
    /// </summary>
    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var userId = GetUserId();
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;
            
            var performance = await _portfolioService.GetPerformanceAsync(userId, fromDate, toDate);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion
}
