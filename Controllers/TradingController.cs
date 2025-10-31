using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CryptoTrading.Services;
using CryptoTrading.Data;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TradingController : ControllerBase
{
    private readonly ICoinGeckoService _coinGeckoService;
    private readonly ApplicationDbContext _db;
    
    public TradingController(ICoinGeckoService coinGeckoService, ApplicationDbContext db)
    {
        _coinGeckoService = coinGeckoService;
        _db = db;
    }
    
    private int GetUserId() 
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            throw new UnauthorizedAccessException("Invalid user ID");
        return userId;
    }

    /// <summary>
    /// Get trading info
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetTradingInfo()
    {
        return Ok(new { message = "Trading API - Ready for implementation", endpoints = new[] { "balances", "orders", "history", "dashboard" } });
    }

    /// <summary>
    /// Get dashboard data (legacy endpoint - kept for compatibility)
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var userId = GetUserId();
            var summary = await GetDashboardSummaryData(userId);
            var navHistory = await GetDashboardNavHistoryData(userId, null);
            var pnlHistory = await GetDashboardPnlHistoryData(userId, "hourly", null);
            
            var dashboardData = new DashboardDto
            {
                TotalBalance = summary.TotalBalance,
                TotalBalanceChange = summary.TotalBalanceChange,
                TotalBalanceChangePercent = summary.TotalBalanceChangePercent,
                TodayPnl = summary.TodayPnl,
                TodayPnlPercent = summary.TodayPnlPercent,
                AvailableBalance = summary.AvailableBalance,
                AvailableBalancePercent = summary.AvailableBalancePercent,
                OpenOrders = summary.OpenOrdersCount,
                OpenOrdersBuy = summary.OpenOrdersBuy,
                OpenOrdersSell = summary.OpenOrdersSell,
                RecentOrders = new List<OrderDto>(),
                TopHoldings = new List<HoldingDto>(),
                NavHistory = navHistory.Data.Select(d => new NavHistoryDto { Date = d.Date, Value = d.Value }).ToList(),
                PnlHistory = pnlHistory.Data.Select(d => new PnlHistoryDto { Time = d.Time, Pnl = d.Pnl }).ToList()
            };

            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get dashboard summary (NAV, TodayPnL, AvailableBalance, OpenOrdersCount)
    /// </summary>
    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummaryEndpoint()
    {
        try
        {
            var userId = GetUserId();
            var summary = await GetDashboardSummaryData(userId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get NAV history for the last 30 days
    /// </summary>
    [HttpGet("dashboard/nav")]
    public async Task<IActionResult> GetDashboardNav([FromQuery] string? from)
    {
        try
        {
            var userId = GetUserId();
            var navHistory = await GetDashboardNavHistoryData(userId, from);
            return Ok(navHistory);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get PnL history for a specific date
    /// </summary>
    [HttpGet("dashboard/pnl")]
    public async Task<IActionResult> GetDashboardPnl([FromQuery] string granularity = "hourly", [FromQuery] string? date = null)
    {
        try
        {
            var userId = GetUserId();
            var pnlHistory = await GetDashboardPnlHistoryData(userId, granularity, date);
            return Ok(pnlHistory);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Helper methods
    private async Task<DashboardSummaryDto> GetDashboardSummaryData(int userId)
    {
        // Get wallets and calculate total balance
        var wallets = await _db.Wallets
            .Where(w => w.UserId == userId)
            .Include(w => w.Cryptocurrency)
            .ToListAsync();

        var walletIds = wallets.Select(w => w.Id).ToList();
        var movements = await _db.WalletMovements
            .Where(m => walletIds.Contains(m.WalletId))
            .GroupBy(m => m.WalletId)
            .Select(g => new { WalletId = g.Key, Balance = g.Sum(m => m.Amount) })
            .ToDictionaryAsync(x => x.WalletId, x => x.Balance);

        var marketData = await _coinGeckoService.GetMarketDataAsync();
        
        // Calculate USD balance from wallet movements
        var usdWallet = wallets.FirstOrDefault(w => w.AssetType == "FIAT" && w.CurrencyCode == "USD");
        var usdBalance = 0m;
        if (usdWallet != null)
        {
            usdBalance = movements.GetValueOrDefault(usdWallet.Id, 0m);
        }

        var totalBalance = usdBalance;
        var availableBalance = usdBalance;
        
        // Calculate crypto balances and their USD values
        foreach (var wallet in wallets.Where(w => w.AssetType == "COIN" && w.Cryptocurrency != null))
        {
            var balance = movements.GetValueOrDefault(wallet.Id, 0m);
            var crypto = marketData.FirstOrDefault(c => 
                c.Symbol.Equals(wallet.Cryptocurrency.Symbol, StringComparison.OrdinalIgnoreCase));
            var price = crypto?.CurrentPrice ?? 0m;
            var valueUsd = balance * price;
            
            totalBalance += valueUsd;
            availableBalance += valueUsd;
        }

        // Calculate previous day's total balance for change calculation
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var previousTotalBalance = 0m;
        
        // Get USD balance as of yesterday
        if (usdWallet != null)
        {
            var usdMovementsYesterday = await _db.WalletMovements
                .Where(m => m.WalletId == usdWallet.Id && m.CreatedAt.Date <= yesterday)
                .SumAsync(m => m.Amount);
            previousTotalBalance = usdMovementsYesterday;
        }

        // Get positions (net quantity per cryptocurrency) as of yesterday
        var allTrades = await _db.Trades
            .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId && t.CreatedAt.Date <= yesterday))
            .Include(t => t.Order)
            .ToListAsync();

        var positions = allTrades
            .GroupBy(t => t.CryptocurrencyId)
            .Select(g => new
            {
                CryptocurrencyId = g.Key,
                QtyCoin = g.Sum(t => t.Order.Side == "BUY" ? t.QuantityCoin : -t.QuantityCoin)
            })
            .Where(p => p.QtyCoin != 0)
            .ToList();

        // Add crypto values as of yesterday
        foreach (var pos in positions)
        {
            var crypto = await _db.Cryptocurrencies.FindAsync(pos.CryptocurrencyId);
            if (crypto != null)
            {
                var latestPrice = await _db.CryptoPrices
                    .Where(p => p.CryptocurrencyId == pos.CryptocurrencyId && p.CollectedAtUtc.Date <= yesterday)
                    .OrderByDescending(p => p.CollectedAtUtc)
                    .Select(p => p.PriceUsd)
                    .FirstOrDefaultAsync();

                previousTotalBalance += pos.QtyCoin * (latestPrice > 0 ? latestPrice : 0);
            }
        }

        // Get open orders count
        var openOrders = await _db.Orders
            .Where(o => o.UserId == userId && (o.Status == "NEW" || o.Status == "PARTIAL"))
            .ToListAsync();

        var openOrdersBuy = openOrders.Count(o => o.Side == "BUY");
        var openOrdersSell = openOrders.Count(o => o.Side == "SELL");

        // Calculate today's PnL from actual trades
        var today = DateTime.UtcNow.Date;
        var todayTrades = await _db.Trades
            .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId && t.CreatedAt.Date == today))
            .Include(t => t.Order)
            .ToListAsync();

        var todayPnl = todayTrades.Sum(t => 
        {
            if (t.Order.Side == "SELL")
                return (t.PriceUsd * t.QuantityCoin) - t.FeeUsd;
            else // BUY
                return -((t.PriceUsd * t.QuantityCoin) + t.FeeUsd);
        });

        var todayPnlPercent = totalBalance > 0 ? (todayPnl / totalBalance) * 100m : 0m;

        var totalBalanceChange = totalBalance - previousTotalBalance;
        var totalBalanceChangePercent = previousTotalBalance > 0 
            ? (totalBalanceChange / previousTotalBalance) * 100m 
            : 0m;

        return new DashboardSummaryDto
        {
            TotalBalance = totalBalance,
            TotalBalanceChange = totalBalanceChange,
            TotalBalanceChangePercent = totalBalanceChangePercent,
            TodayPnl = todayPnl,
            TodayPnlPercent = todayPnlPercent,
            AvailableBalance = availableBalance,
            AvailableBalancePercent = totalBalance > 0 ? (availableBalance / totalBalance) * 100m : 0m,
            OpenOrdersCount = openOrders.Count,
            OpenOrdersBuy = openOrdersBuy,
            OpenOrdersSell = openOrdersSell
        };
    }

    private async Task<DashboardNavHistoryDto> GetDashboardNavHistoryData(int userId, string? from)
    {
        var fromDate = string.IsNullOrEmpty(from) 
            ? DateTime.UtcNow.AddDays(-29).Date 
            : DateTime.Parse(from).Date;

        var data = new List<DashboardNavDataPointDto>();
        
        // Get all trades up to now
        var allTrades = await _db.Trades
            .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId))
            .Include(t => t.Order)
            .Include(t => t.Cryptocurrency)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        // Get positions (net quantity per cryptocurrency)
        var positions = allTrades
            .GroupBy(t => t.CryptocurrencyId)
            .Select(g => new
            {
                CryptocurrencyId = g.Key,
                QtyCoin = g.Sum(t => t.Order.Side == "BUY" ? t.QuantityCoin : -t.QuantityCoin)
            })
            .Where(p => p.QtyCoin != 0)
            .ToList();

        // For each day in the range, calculate NAV using latest price on or before that date
        for (int i = 29; i >= 0; i--)
        {
            var date = fromDate.AddDays(i);
            decimal nav = 0m;

            foreach (var pos in positions)
            {
                // Get latest price on or before this date
                var latestPrice = await _db.CryptoPrices
                    .Where(p => p.CryptocurrencyId == pos.CryptocurrencyId && p.CollectedAtUtc.Date <= date)
                    .OrderByDescending(p => p.CollectedAtUtc)
                    .Select(p => p.PriceUsd)
                    .FirstOrDefaultAsync();

                nav += pos.QtyCoin * (latestPrice > 0 ? latestPrice : 0);
            }

            // Add USD balance (FIAT wallets)
            var usdWallets = await _db.Wallets
                .Where(w => w.UserId == userId && w.AssetType == "FIAT" && w.CurrencyCode == "USD")
                .ToListAsync();

            var usdWalletIds = usdWallets.Select(w => w.Id).ToList();
            if (usdWalletIds.Any())
            {
                var usdBalance = await _db.WalletMovements
                    .Where(m => usdWalletIds.Contains(m.WalletId) && m.CreatedAt.Date <= date)
                    .GroupBy(m => m.WalletId)
                    .Select(g => g.Sum(m => m.Amount))
                    .SumAsync();

                nav += usdBalance;
            }

            data.Add(new DashboardNavDataPointDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Value = Math.Round(nav, 2)
            });
        }

        return new DashboardNavHistoryDto
        {
            From = fromDate.ToString("yyyy-MM-dd"),
            To = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            Data = data
        };
    }

    private async Task<DashboardPnlHistoryDto> GetDashboardPnlHistoryData(int userId, string granularity, string? date)
    {
        var targetDate = string.IsNullOrEmpty(date)
            ? DateTime.UtcNow.Date
            : DateTime.Parse(date).Date;

        var data = new List<DashboardPnlDataPointDto>();

        if (granularity == "hourly")
        {
            // Get all trades for the target date
            var dayTrades = await _db.Trades
                .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId) && 
                           t.CreatedAt.Date == targetDate)
                .Include(t => t.Order)
                .ToListAsync();

            // Group by hour and calculate PnL
            for (int hour = 0; hour < 24; hour++)
            {
                var hourStart = targetDate.AddHours(hour);
                var hourEnd = hourStart.AddHours(1);

                var hourTrades = dayTrades
                    .Where(t => t.CreatedAt >= hourStart && t.CreatedAt < hourEnd)
                    .ToList();

                var pnl = hourTrades.Sum(t =>
                {
                    if (t.Order.Side == "SELL")
                        return (t.PriceUsd * t.QuantityCoin) - t.FeeUsd;
                    else // BUY
                        return -((t.PriceUsd * t.QuantityCoin) + t.FeeUsd);
                });

                data.Add(new DashboardPnlDataPointDto
                {
                    Time = $"{hour:D2}:00",
                    Pnl = Math.Round(pnl, 2)
                });
            }
        }

        return new DashboardPnlHistoryDto
        {
            Granularity = granularity,
            Date = targetDate.ToString("yyyy-MM-dd"),
            Data = data
        };
    }

    /// <summary>
    /// Get order book for a trading pair
    /// </summary>
    [HttpGet("orderbook/{symbol}")]
    public async Task<IActionResult> GetOrderBook(string symbol)
    {
        try
        {
            var userId = GetUserId();
            
            // Get current market price from CoinGecko
            var marketData = await _coinGeckoService.GetMarketDataAsync();
            var baseSymbol = symbol.Split('/')[0].ToUpper();
            var crypto = marketData.FirstOrDefault(c => c.Symbol.Equals(baseSymbol, StringComparison.OrdinalIgnoreCase));
            
            if (crypto == null)
            {
                return NotFound(new { message = $"Cryptocurrency '{baseSymbol}' not found" });
            }
            
            var currentPrice = crypto.CurrentPrice ?? 0m;
            var priceChange24h = crypto.PriceChange24h ?? 0m;
            var priceChangePercentage24h = crypto.PriceChangePercentage24h ?? 0m;
            
            // Generate order book around current price (mock order book)
            var asks = new List<OrderBookLevelDto>();
            var bids = new List<OrderBookLevelDto>();
            
            // Generate sell orders (asks) - above current price
            for (int i = 0; i < 5; i++)
            {
                var price = currentPrice * (1 + (0.001m * (i + 1))); // 0.1% above per level
                asks.Add(new OrderBookLevelDto
                {
                    Price = price,
                    Amount = (decimal)(0.2 + i * 0.1),
                    Total = price * (decimal)(0.2 + i * 0.1)
                });
            }
            
            // Generate buy orders (bids) - below current price
            for (int i = 0; i < 5; i++)
            {
                var price = currentPrice * (1 - (0.001m * (i + 1))); // 0.1% below per level
                bids.Add(new OrderBookLevelDto
                {
                    Price = price,
                    Amount = (decimal)(0.3 + i * 0.1),
                    Total = price * (decimal)(0.3 + i * 0.1)
                });
            }
            
            var orderBook = new OrderBookDto
            {
                Symbol = symbol,
                CurrentPrice = currentPrice,
                PriceChange24h = priceChange24h,
                PriceChangePercentage24h = priceChangePercentage24h,
                Asks = asks.OrderByDescending(a => a.Price).ToList(),
                Bids = bids.OrderByDescending(b => b.Price).ToList(),
                LastUpdated = DateTime.UtcNow
            };
            
            return Ok(orderBook);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get user balances
    /// </summary>
    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances()
    {
        try
        {
            var userId = GetUserId();
            
            // Get all wallets for the user
            var wallets = await _db.Wallets
                .Where(w => w.UserId == userId)
                .Include(w => w.Cryptocurrency)
                .ToListAsync();
            
            // Get wallet movements to calculate balances
            var walletIds = wallets.Select(w => w.Id).ToList();
            var movements = await _db.WalletMovements
                .Where(m => walletIds.Contains(m.WalletId))
                .GroupBy(m => m.WalletId)
                .Select(g => new { WalletId = g.Key, Balance = g.Sum(m => m.Amount) })
                .ToDictionaryAsync(x => x.WalletId, x => x.Balance);
            
            // Get current crypto prices for calculation
            var marketData = await _coinGeckoService.GetMarketDataAsync();
            
            var walletBalances = new List<WalletBalanceDto>();
            decimal totalBalance = 0m;
            decimal availableBalance = 0m;
            decimal lockedBalance = 0m;
            
            foreach (var wallet in wallets)
            {
                var balance = movements.GetValueOrDefault(wallet.Id, 0m);
                var locked = 0m; // TODO: Calculate locked from OrderHolds
                
                decimal valueUsd = 0m;
                string symbol = "";
                
                if (wallet.AssetType == "FIAT")
                {
                    symbol = wallet.CurrencyCode ?? "USD";
                    valueUsd = balance; // For FIAT, 1:1 with USD
                }
                else if (wallet.AssetType == "COIN" && wallet.Cryptocurrency != null)
                {
                    symbol = wallet.Cryptocurrency.Symbol.ToUpper();
                    var crypto = marketData.FirstOrDefault(c => c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
                    var price = crypto?.CurrentPrice ?? 0m;
                    valueUsd = balance * price;
                }
                
                walletBalances.Add(new WalletBalanceDto
                {
                    Symbol = symbol,
                    Available = balance - locked,
                    Locked = locked,
                    Total = balance,
                    ValueUsd = valueUsd
                });
                
                totalBalance += valueUsd;
                availableBalance += valueUsd - (locked * (wallet.AssetType == "COIN" && wallet.Cryptocurrency != null 
                    ? (marketData.FirstOrDefault(c => c.Symbol.Equals(wallet.Cryptocurrency.Symbol, StringComparison.OrdinalIgnoreCase))?.CurrentPrice ?? 0m) 
                    : 1m));
                lockedBalance += locked * (wallet.AssetType == "COIN" && wallet.Cryptocurrency != null 
                    ? (marketData.FirstOrDefault(c => c.Symbol.Equals(wallet.Cryptocurrency.Symbol, StringComparison.OrdinalIgnoreCase))?.CurrentPrice ?? 0m) 
                    : 1m);
            }
            
            var balances = new TradingBalancesDto
            {
                TotalBalance = totalBalance,
                AvailableBalance = availableBalance,
                LockedBalance = lockedBalance,
                Wallets = walletBalances
            };
            
            return Ok(balances);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Place new order
    /// </summary>
    [HttpPost("orders")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        try
        {
            var userId = GetUserId();
            
            // Parse symbol (e.g., "BTC/USDT" -> "BTC")
            var symbolParts = dto.Symbol.Split('/');
            if (symbolParts.Length != 2)
            {
                return BadRequest(new { message = "Invalid symbol format. Expected format: SYMBOL/USDT" });
            }
            
            var coinSymbol = symbolParts[0].ToUpper();
            
            // Find cryptocurrency
            var crypto = await _db.Cryptocurrencies
                .FirstOrDefaultAsync(c => c.Symbol.ToUpper() == coinSymbol);
            
            if (crypto == null)
            {
                return NotFound(new { message = $"Cryptocurrency '{coinSymbol}' not found" });
            }
            
            // Create order
            var order = new Order
            {
                UserId = userId,
                CryptocurrencyId = crypto.Id,
                Side = dto.Side.ToUpper(),
                Type = dto.Type.ToUpper(),
                PriceUsd = dto.Price,
                QuantityCoin = dto.Quantity,
                FilledQty = 0,
                Status = "NEW",
                CreatedAt = DateTime.UtcNow
            };
            
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            
            // Return DTO
            var orderDto = new OrderDto
            {
                Id = order.Id.ToString(),
                Symbol = dto.Symbol,
                Side = order.Side,
                Type = order.Type,
                Quantity = order.QuantityCoin,
                Price = order.PriceUsd,
                Filled = order.FilledQty,
                Remaining = order.QuantityCoin - order.FilledQty,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt
            };
            
            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrder(ulong id)
    {
        try
        {
            var userId = GetUserId();
            
            var order = await _db.Orders
                .Where(o => o.Id == id && o.UserId == userId)
                .Include(o => o.Cryptocurrency)
                .FirstOrDefaultAsync();
            
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }
            
            var orderDto = new OrderDto
            {
                Id = order.Id.ToString(),
                Symbol = $"{order.Cryptocurrency.Symbol.ToUpper()}/USDT",
                Side = order.Side,
                Type = order.Type,
                Quantity = order.QuantityCoin,
                Price = order.PriceUsd,
                Filled = order.FilledQty,
                Remaining = order.QuantityCoin - order.FilledQty,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt
            };
            
            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get user orders
    /// </summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var userId = GetUserId();
            
            // Get real orders from database
            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Cryptocurrency)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id.ToString(),
                Symbol = $"{o.Cryptocurrency.Symbol.ToUpper()}/USDT",
                Side = o.Side,
                Type = o.Type,
                Quantity = o.QuantityCoin,
                Price = o.PriceUsd,
                Filled = o.FilledQty,
                Remaining = o.QuantityCoin - o.FilledQty,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt ?? o.CreatedAt
            }).ToList();
            
            return Ok(orderDtos);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

// DTOs
public record PlaceOrderDto(string Symbol, string Side, string Type, decimal Quantity, decimal? Price = null);

public class DashboardDto
{
    public decimal TotalBalance { get; set; }
    public decimal TotalBalanceChange { get; set; }
    public decimal TotalBalanceChangePercent { get; set; }
    public decimal TodayPnl { get; set; }
    public decimal TodayPnlPercent { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal AvailableBalancePercent { get; set; }
    public int OpenOrders { get; set; }
    public int OpenOrdersBuy { get; set; }
    public int OpenOrdersSell { get; set; }
    public List<OrderDto> RecentOrders { get; set; } = new();
    public List<HoldingDto> TopHoldings { get; set; } = new();
    public List<NavHistoryDto> NavHistory { get; set; } = new();
    public List<PnlHistoryDto> PnlHistory { get; set; } = new();
}

public class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal Filled { get; set; }
    public decimal Remaining { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class HoldingDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal ValueUsd { get; set; }
    public decimal Change24h { get; set; }
}

public class NavHistoryDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class PnlHistoryDto
{
    public string Time { get; set; } = string.Empty;
    public decimal Pnl { get; set; }
}

public class OrderBookDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PriceChange24h { get; set; }
    public decimal PriceChangePercentage24h { get; set; }
    public List<OrderBookLevelDto> Asks { get; set; } = new();
    public List<OrderBookLevelDto> Bids { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class OrderBookLevelDto
{
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public decimal Total { get; set; }
}

public class TradingBalancesDto
{
    public decimal TotalBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal LockedBalance { get; set; }
    public List<WalletBalanceDto> Wallets { get; set; } = new();
}

public class WalletBalanceDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Available { get; set; }
    public decimal Locked { get; set; }
    public decimal Total { get; set; }
    public decimal ValueUsd { get; set; }
}

// Dashboard DTOs
public class DashboardSummaryDto
{
    public decimal TotalBalance { get; set; }
    public decimal TotalBalanceChange { get; set; }
    public decimal TotalBalanceChangePercent { get; set; }
    public decimal TodayPnl { get; set; }
    public decimal TodayPnlPercent { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal AvailableBalancePercent { get; set; }
    public int OpenOrdersCount { get; set; }
    public int OpenOrdersBuy { get; set; }
    public int OpenOrdersSell { get; set; }
}

public class DashboardNavDataPointDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class DashboardNavHistoryDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public List<DashboardNavDataPointDto> Data { get; set; } = new();
}

public class DashboardPnlDataPointDto
{
    public string Time { get; set; } = string.Empty;
    public decimal Pnl { get; set; }
}

public class DashboardPnlHistoryDto
{
    public string Granularity { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public List<DashboardPnlDataPointDto> Data { get; set; } = new();
}
