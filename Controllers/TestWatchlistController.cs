using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Controllers;

/// <summary>
/// Test controller for Watchlist features - No database required
/// Requires authentication to test JWT flow
/// </summary>
[ApiController]
[Route("api/test/[controller]")]
[Authorize]
public class TestWatchlistController : ControllerBase
{
    private static List<TestWatchlist> _watchlists = new();

    /// <summary>
    /// UC 40: Get all watchlists (Mock data)
    /// </summary>
    [HttpGet]
    public IActionResult GetAllWatchlists()
    {
        if (!_watchlists.Any())
        {
            // Create default watchlist
            _watchlists.Add(new TestWatchlist
            {
                Id = Guid.NewGuid(),
                Name = "My Watchlist",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Coins = new List<TestCoin>
                {
                    new TestCoin { Symbol = "BTC", Name = "Bitcoin", Price = 107000m, Change24h = 2.5m },
                    new TestCoin { Symbol = "ETH", Name = "Ethereum", Price = 3900m, Change24h = -1.2m },
                    new TestCoin { Symbol = "BNB", Name = "BNB", Price = 650m, Change24h = 3.1m }
                }
            });
        }

        var result = _watchlists.Select(w => new
        {
            w.Id,
            w.Name,
            w.IsDefault,
            CoinCount = w.Coins.Count,
            w.CreatedAt,
            w.UpdatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// UC 36: Create new watchlist (Mock)
    /// </summary>
    [HttpPost]
    public IActionResult CreateWatchlist([FromBody] CreateWatchlistDto dto)
    {
        if (_watchlists.Count >= 3)
        {
            return BadRequest(new { message = "Watchlist limit reached (3 for Basic plan)" });
        }

        if (dto.IsDefault)
        {
            foreach (var w in _watchlists) w.IsDefault = false;
        }

        var newWatchlist = new TestWatchlist
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            IsDefault = dto.IsDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Coins = new List<TestCoin>()
        };

        _watchlists.Add(newWatchlist);

        return CreatedAtAction(nameof(GetWatchlist), new { id = newWatchlist.Id }, new
        {
            newWatchlist.Id,
            newWatchlist.Name,
            newWatchlist.IsDefault,
            CoinCount = 0,
            newWatchlist.CreatedAt,
            newWatchlist.UpdatedAt,
            Coins = new List<object>()
        });
    }

    /// <summary>
    /// Get watchlist by ID (Mock)
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetWatchlist(Guid id)
    {
        var watchlist = _watchlists.FirstOrDefault(w => w.Id == id);
        if (watchlist == null)
            return NotFound(new { message = "Watchlist not found" });

        return Ok(new
        {
            watchlist.Id,
            watchlist.Name,
            watchlist.IsDefault,
            CoinCount = watchlist.Coins.Count,
            watchlist.CreatedAt,
            watchlist.UpdatedAt,
            Coins = watchlist.Coins.Select(c => new
            {
                c.Symbol,
                c.Name,
                IconUrl = "",
                CurrentPrice = c.Price,
                PriceChange24h = c.Price * c.Change24h / 100,
                PriceChangePercent24h = c.Change24h,
                AddedAt = watchlist.CreatedAt
            })
        });
    }

    /// <summary>
    /// UC 45: Get default watchlist (Mock)
    /// </summary>
    [HttpGet("default")]
    public IActionResult GetDefaultWatchlist()
    {
        var watchlist = _watchlists.FirstOrDefault(w => w.IsDefault);
        if (watchlist == null)
            return NotFound(new { message = "No default watchlist found" });

        return Ok(new
        {
            watchlist.Id,
            watchlist.Name,
            watchlist.IsDefault,
            CoinCount = watchlist.Coins.Count,
            watchlist.CreatedAt,
            watchlist.UpdatedAt,
            Coins = watchlist.Coins
        });
    }

    /// <summary>
    /// UC 41: Rename watchlist (Mock)
    /// </summary>
    [HttpPut("{id}/rename")]
    public IActionResult RenameWatchlist(Guid id, [FromBody] RenameWatchlistDto dto)
    {
        var watchlist = _watchlists.FirstOrDefault(w => w.Id == id);
        if (watchlist == null)
            return NotFound(new { message = "Watchlist not found" });

        watchlist.Name = dto.Name;
        watchlist.UpdatedAt = DateTime.UtcNow;

        return Ok(new
        {
            watchlist.Id,
            watchlist.Name,
            watchlist.IsDefault,
            CoinCount = watchlist.Coins.Count,
            watchlist.CreatedAt,
            watchlist.UpdatedAt
        });
    }

    /// <summary>
    /// UC 42: Delete watchlist (Mock)
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult DeleteWatchlist(Guid id)
    {
        var watchlist = _watchlists.FirstOrDefault(w => w.Id == id);
        if (watchlist == null)
            return NotFound(new { message = "Watchlist not found" });

        if (_watchlists.Count <= 1)
            return BadRequest(new { message = "Cannot delete the last watchlist" });

        _watchlists.Remove(watchlist);

        if (watchlist.IsDefault && _watchlists.Any())
        {
            _watchlists[0].IsDefault = true;
        }

        return NoContent();
    }

    /// <summary>
    /// UC 37/38: Add coin to watchlist (Mock)
    /// </summary>
    [HttpPost("{id}/coins")]
    public IActionResult AddCoinToWatchlist(Guid id, [FromBody] AddCoinToWatchlistDto dto)
    {
        var watchlist = _watchlists.FirstOrDefault(w => w.Id == id);
        if (watchlist == null)
            return NotFound(new { message = "Watchlist not found" });

        if (watchlist.Coins.Any(c => c.Symbol.Equals(dto.CoinSymbol, StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { message = "Coin already exists in watchlist" });

        var mockPrices = new Dictionary<string, (string Name, decimal Price, decimal Change)>
        {
            {"BTC", ("Bitcoin", 107000m, 2.5m)},
            {"ETH", ("Ethereum", 3900m, -1.2m)},
            {"BNB", ("BNB", 650m, 3.1m)},
            {"XRP", ("XRP", 2.3m, 5.2m)},
            {"SOL", ("Solana", 180m, -2.1m)},
            {"ADA", ("Cardano", 1.2m, 4.3m)},
            {"DOGE", ("Dogecoin", 0.35m, 8.5m)}
        };

        var symbol = dto.CoinSymbol.ToUpper();
        var coinData = mockPrices.ContainsKey(symbol) 
            ? mockPrices[symbol] 
            : (symbol, 100m, 0m);

        watchlist.Coins.Add(new TestCoin
        {
            Symbol = symbol,
            Name = coinData.Item1,
            Price = coinData.Item2,
            Change24h = coinData.Item3
        });

        watchlist.UpdatedAt = DateTime.UtcNow;

        return Ok(new { message = $"Added {dto.CoinSymbol} to watchlist" });
    }

    /// <summary>
    /// UC 37: Add coin to default watchlist (Mock)
    /// </summary>
    [HttpPost("default/coins")]
    public IActionResult AddCoinToDefaultWatchlist([FromBody] AddCoinToWatchlistDto dto)
    {
        var watchlist = _watchlists.FirstOrDefault(w => w.IsDefault);
        if (watchlist == null)
            return NotFound(new { message = "No default watchlist found" });

        return AddCoinToWatchlist(watchlist.Id, dto);
    }

    /// <summary>
    /// UC 39: Remove coin from watchlist (Mock)
    /// </summary>
    [HttpDelete("{id}/coins/{symbol}")]
    public IActionResult RemoveCoinFromWatchlist(Guid id, string symbol)
    {
        var watchlist = _watchlists.FirstOrDefault(w => w.Id == id);
        if (watchlist == null)
            return NotFound(new { message = "Watchlist not found" });

        var coin = watchlist.Coins.FirstOrDefault(c => c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (coin == null)
            return NotFound(new { message = "Coin not found in watchlist" });

        watchlist.Coins.Remove(coin);
        watchlist.UpdatedAt = DateTime.UtcNow;

        return Ok(new { message = $"Removed {symbol} from watchlist" });
    }

    /// <summary>
    /// UC 43: Get realtime updates (Mock)
    /// </summary>
    [HttpGet("{id}/realtime")]
    public IActionResult GetRealtimeUpdates(Guid id)
    {
        var watchlist = _watchlists.FirstOrDefault(w => w.Id == id);
        if (watchlist == null)
            return NotFound(new { message = "Watchlist not found" });

        var random = new Random();
        var updates = watchlist.Coins.Select(c => new
        {
            c.Symbol,
            NewPrice = c.Price * (1 + (decimal)(random.NextDouble() * 0.02 - 0.01)), // ±1% random change
            PriceChange = c.Price * c.Change24h / 100,
            PriceChangePercent = c.Change24h,
            UpdatedAt = DateTime.UtcNow
        });

        return Ok(new
        {
            WatchlistId = id,
            Updates = updates
        });
    }

    /// <summary>
    /// UC 44: Get watchlist quota (Mock)
    /// </summary>
    [HttpGet("quota")]
    public IActionResult GetQuota()
    {
        return Ok(new
        {
            CurrentCount = _watchlists.Count,
            MaxAllowed = 3,
            SubscriptionTier = "Basic",
            CanCreateMore = _watchlists.Count < 3
        });
    }

    /// <summary>
    /// Clear all test data
    /// </summary>
    [HttpDelete("clear")]
    public IActionResult ClearAll()
    {
        _watchlists.Clear();
        return Ok(new { message = "All test data cleared" });
    }
}

// Test models
public class TestWatchlist
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<TestCoin> Coins { get; set; } = new();
}

public class TestCoin
{
    public string Symbol { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Change24h { get; set; }
}
