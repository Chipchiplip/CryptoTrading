using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CryptoTrading.Services;
using CryptoTrading.Services.Trading;
using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Controllers;

/// <summary>
/// Controller for trading operations including orders, balances, and dashboard
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TradingController : ControllerBase
{
    private readonly ICoinGeckoService _coinGeckoService;
    private readonly ITradingService _tradingService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TradingController> _logger;
    
    public TradingController(
        ICoinGeckoService coinGeckoService,
        ITradingService tradingService,
        ApplicationDbContext db,
        ILogger<TradingController> logger)
    {
        _coinGeckoService = coinGeckoService;
        _tradingService = tradingService;
        _db = db;
        _logger = logger;
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
        var userId = GetUserId();
        _logger.LogDebug("Fetching dashboard for user {UserId}", userId);
        
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

    /// <summary>
    /// Get dashboard summary (NAV, TodayPnL, AvailableBalance, OpenOrdersCount)
    /// </summary>
    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummaryEndpoint()
    {
        var userId = GetUserId();
        _logger.LogDebug("Fetching dashboard summary for user {UserId}", userId);
        
        var summary = await GetDashboardSummaryData(userId);
        return Ok(summary);
    }

    /// <summary>
    /// Get NAV history for the last 30 days
    /// </summary>
    [HttpGet("dashboard/nav")]
    public async Task<IActionResult> GetDashboardNav([FromQuery] string? from)
    {
        var userId = GetUserId();
        _logger.LogDebug("Fetching NAV history for user {UserId}", userId);
        
        var navHistory = await GetDashboardNavHistoryData(userId, from);
        return Ok(navHistory);
    }

    /// <summary>
    /// Get PnL history for a specific date
    /// </summary>
    [HttpGet("dashboard/pnl")]
    public async Task<IActionResult> GetDashboardPnl([FromQuery] string granularity = "hourly", [FromQuery] string? date = null)
    {
        var userId = GetUserId();
        _logger.LogDebug("Fetching PnL history for user {UserId}", userId);
        
        var pnlHistory = await GetDashboardPnlHistoryData(userId, granularity, date);
        return Ok(pnlHistory);
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
        
        // Get locked balances from active order holds
        var orderHolds = await _db.OrderHolds
            .Where(h => walletIds.Contains(h.WalletId) && h.ReleasedAt == null)
            .GroupBy(h => h.WalletId)
            .Select(g => new { WalletId = g.Key, LockedAmount = g.Sum(h => h.Amount) })
            .ToDictionaryAsync(x => x.WalletId, x => x.LockedAmount);
        
        // Calculate USD balance from wallet movements
        var usdWallet = wallets.FirstOrDefault(w => w.AssetType == "FIAT" && w.CurrencyCode == "USD");
        var usdBalance = 0m;
        var usdLocked = 0m;
        if (usdWallet != null)
        {
            usdBalance = movements.GetValueOrDefault(usdWallet.Id, 0m);
            usdLocked = orderHolds.GetValueOrDefault(usdWallet.Id, 0m);
        }

        var totalBalance = usdBalance;
        // Available Balance = USD available for trading (not locked in orders)
        var availableBalance = usdBalance - usdLocked;
        
        // Calculate crypto balances and their USD values
        foreach (var wallet in wallets.Where(w => w.AssetType == "COIN" && w.Cryptocurrency != null))
        {
            var balance = movements.GetValueOrDefault(wallet.Id, 0m);
            var crypto = marketData.FirstOrDefault(c => 
                c.Symbol.Equals(wallet.Cryptocurrency!.Symbol, StringComparison.OrdinalIgnoreCase));
            var price = crypto?.CurrentPrice ?? 0m;
            var valueUsd = balance * price;
            
            totalBalance += valueUsd;
            // Note: Available Balance does NOT include crypto value, only USD available for trading
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

        // Add crypto values as of yesterday - optimized to fetch all prices in one query
        if (positions.Any())
        {
            var cryptoIds = positions.Select(p => p.CryptocurrencyId).Distinct().ToList();
            
            // Fetch all relevant prices at once
            var yesterdayPrices = await _db.CryptoPrices
                .Where(p => cryptoIds.Contains(p.CryptocurrencyId) && p.CollectedAtUtc.Date <= yesterday)
                .GroupBy(p => p.CryptocurrencyId)
                .Select(g => new
                {
                    CryptocurrencyId = g.Key,
                    Price = g.OrderByDescending(p => p.CollectedAtUtc).Select(p => p.PriceUsd).FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.CryptocurrencyId, x => x.Price);
            
            foreach (var pos in positions)
            {
                var latestPrice = yesterdayPrices.GetValueOrDefault(pos.CryptocurrencyId, 0m);
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
        var toDate = DateTime.UtcNow.Date;
        var defaultStart = toDate.AddDays(-29);
        DateTime fromDate;
        if (!string.IsNullOrEmpty(from) && DateTime.TryParse(from, out var parsed))
        {
            fromDate = parsed.Date;
        }
        else
        {
            fromDate = defaultStart;
        }

        if (fromDate > toDate)
        {
            fromDate = toDate;
        }

        var maxWindowStart = toDate.AddDays(-29);
        if (fromDate < maxWindowStart)
        {
            fromDate = maxWindowStart;
        }

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

        // Preload all price data for the date range and cryptocurrencies to avoid N+1 queries
        var cryptoIds = positions.Select(p => p.CryptocurrencyId).Distinct().ToList();
        var allPrices = new Dictionary<(int CryptoId, DateTime Date), decimal>();
        
        if (cryptoIds.Any())
        {
            var priceData = await _db.CryptoPrices
                .Where(p => cryptoIds.Contains(p.CryptocurrencyId) && 
                           p.CollectedAtUtc.Date >= fromDate && 
                           p.CollectedAtUtc.Date <= toDate)
                .Select(p => new { p.CryptocurrencyId, Date = p.CollectedAtUtc.Date, p.PriceUsd, p.CollectedAtUtc })
                .ToListAsync();
            
            // Group by crypto and date, taking the latest price for each day
            var groupedPrices = priceData
                .GroupBy(p => new { p.CryptocurrencyId, p.Date })
                .Select(g => new
                {
                    g.Key.CryptocurrencyId,
                    g.Key.Date,
                    Price = g.OrderByDescending(p => p.CollectedAtUtc).First().PriceUsd
                });
            
            foreach (var price in groupedPrices)
            {
                allPrices[(price.CryptocurrencyId, price.Date)] = price.Price;
            }
        }

        // Preload USD wallet ids once
        var usdWalletIds = await _db.Wallets
            .Where(w => w.UserId == userId && w.AssetType == "FIAT" && w.CurrencyCode == "USD")
            .Select(w => w.Id)
            .ToListAsync();

        var usdDailyChanges = new Dictionary<DateTime, decimal>();
        decimal usdBalanceBeforeStart = 0m;

        if (usdWalletIds.Any())
        {
            var usdMovements = await _db.WalletMovements
                .Where(m => usdWalletIds.Contains(m.WalletId) && m.CreatedAt.Date <= toDate)
                .Select(m => new { m.CreatedAt.Date, m.Amount })
                .ToListAsync();

            foreach (var movement in usdMovements.Where(m => m.Date < fromDate))
            {
                usdBalanceBeforeStart += movement.Amount;
            }

            foreach (var grp in usdMovements.Where(m => m.Date >= fromDate).GroupBy(m => m.Date))
            {
                usdDailyChanges[grp.Key] = grp.Sum(x => x.Amount);
            }
        }

        var totalDays = (toDate - fromDate).Days;
        if (totalDays < 0)
        {
            totalDays = 0;
        }
        if (totalDays > 29)
        {
            totalDays = 29;
        }

        decimal runningUsdBalance = usdBalanceBeforeStart;

        // For each day in the range, calculate NAV using preloaded prices
        for (int i = totalDays; i >= 0; i--)
        {
            var date = fromDate.AddDays(i);
            decimal nav = 0m;

            foreach (var pos in positions)
            {
                // Find the latest available price on or before this date
                decimal latestPrice = 0m;
                for (var checkDate = date; checkDate >= fromDate; checkDate = checkDate.AddDays(-1))
                {
                    if (allPrices.TryGetValue((pos.CryptocurrencyId, checkDate), out var price))
                    {
                        latestPrice = price;
                        break;
                    }
                }

                nav += pos.QtyCoin * latestPrice;
            }

            if (usdDailyChanges.TryGetValue(date, out var delta))
            {
                runningUsdBalance += delta;
            }

            nav += runningUsdBalance;

            data.Add(new DashboardNavDataPointDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Value = Math.Round(nav, 2)
            });
        }

        return new DashboardNavHistoryDto
        {
            From = fromDate.ToString("yyyy-MM-dd"),
            To = toDate.ToString("yyyy-MM-dd"),
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
    /// Get order book for a trading pair with real bid/ask aggregation
    /// </summary>
    /// <param name="symbolQuery">Trading pair symbol (e.g., "BTC/USDT") as query parameter</param>
    /// <param name="symbol">Trading pair symbol as route parameter (optional, for backward compatibility)</param>
    /// <param name="depth">Number of price levels to return (default 20)</param>
    [HttpGet("orderbook")]
    public async Task<IActionResult> GetOrderBook([FromQuery] string? symbolQuery = null, [FromRoute] string? symbol = null, [FromQuery] int depth = 20)
    {
        try
        {
            // Support both route parameter and query string for backward compatibility
            // Use query string if provided (more reliable for symbols with /), otherwise use route parameter
            var tradingSymbol = symbolQuery ?? symbol;
            
            // Decode URL-encoded symbols (e.g., BTC%2FUSDT -> BTC/USDT)
            if (!string.IsNullOrEmpty(tradingSymbol))
            {
                tradingSymbol = Uri.UnescapeDataString(tradingSymbol);
            }
            
            if (string.IsNullOrWhiteSpace(tradingSymbol))
            {
                _logger.LogWarning("Order book request missing symbol parameter");
                return BadRequest(new { message = "Symbol is required. Provide it as route parameter (/orderbook/BTC%2FUSDT) or query string (?symbolQuery=BTC%2FUSDT)" });
            }
            
            _logger.LogDebug("Fetching order book for symbol {Symbol}", tradingSymbol);
            var orderBook = await _tradingService.GetOrderBookAsync(tradingSymbol, depth);
            return Ok(orderBook);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid symbol for order book request");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error fetching order book: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching order book");
            throw;
        }
    }

    /// <summary>
    /// Get user holdings (portfolio positions)
    /// </summary>
    [HttpGet("holdings")]
    public async Task<IActionResult> GetHoldings()
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
            
            // Get current crypto prices
            var marketData = await _coinGeckoService.GetMarketDataAsync();
            
            var holdings = new List<HoldingDto>();
            
            foreach (var wallet in wallets.Where(w => w.AssetType == "COIN" && w.Cryptocurrency != null))
            {
                var balance = movements.GetValueOrDefault(wallet.Id, 0m);
                // Skip empty holdings or very small balances (rounding errors)
                if (balance <= 0 || Math.Abs(balance) < 0.00000001m) continue;
                
                var symbol = wallet.Cryptocurrency!.Symbol.ToUpper();
                var crypto = marketData.FirstOrDefault(c => c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
                var price = crypto?.CurrentPrice ?? 0m;
                var valueUsd = balance * price;
                var change24h = crypto?.PriceChangePercentage24h ?? 0m;
                
                holdings.Add(new HoldingDto
                {
                    Symbol = symbol,
                    Name = wallet.Cryptocurrency!.Name ?? symbol,
                    Amount = balance,
                    ValueUsd = valueUsd,
                    Change24h = change24h
                });
            }
            
            // Sort by value descending
            holdings = holdings.OrderByDescending(h => h.ValueUsd).ToList();
            
            return Ok(holdings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving holdings for user");
            throw;
        }
    }

    /// <summary>
    /// Get user balances
    /// </summary>
    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances()
    {
        var userId = GetUserId();
        _logger.LogDebug("Fetching balances for user {UserId}", userId);
            
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
            
            // Get locked balances from active order holds
            var orderHolds = await _db.OrderHolds
                .Where(h => walletIds.Contains(h.WalletId) && h.ReleasedAt == null)
                .GroupBy(h => h.WalletId)
                .Select(g => new { WalletId = g.Key, LockedAmount = g.Sum(h => h.Amount) })
                .ToDictionaryAsync(x => x.WalletId, x => x.LockedAmount);
            
            // Get current crypto prices for calculation
            var marketData = await _coinGeckoService.GetMarketDataAsync();
            
            var walletBalances = new List<WalletBalanceDto>();
            decimal totalBalance = 0m;
            decimal availableBalance = 0m;
            decimal lockedBalance = 0m;
            
            foreach (var wallet in wallets)
            {
                var balance = movements.GetValueOrDefault(wallet.Id, 0m);
                var locked = orderHolds.GetValueOrDefault(wallet.Id, 0m);
                
                // Skip wallets with zero or very small balance (rounding errors)
                // For COIN wallets, also check if value is effectively zero
                if (wallet.AssetType == "COIN" && wallet.Cryptocurrency != null)
                {
                    var coinSymbol = wallet.Cryptocurrency.Symbol.ToUpper();
                    var crypto = marketData.FirstOrDefault(c => c.Symbol.Equals(coinSymbol, StringComparison.OrdinalIgnoreCase));
                    var price = crypto?.CurrentPrice ?? 0m;
                    var coinValueUsd = balance * price;
                    
                    // Skip if balance is zero or very small, or value is effectively zero
                    if (balance <= 0 || Math.Abs(balance) < 0.00000001m || Math.Abs(coinValueUsd) < 0.01m)
                    {
                        continue;
                    }
                }
                else if (wallet.AssetType == "FIAT")
                {
                    // For FIAT, skip if balance is zero or very small
                    if (balance <= 0 || Math.Abs(balance) < 0.01m)
                    {
                        continue;
                    }
                }
                
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

    /// <summary>
    /// Place a new order with validation and balance locking
    /// </summary>
    /// <param name="request">Order placement request</param>
    [HttpPost("orders")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid order request: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(ModelState);
        }
        
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} placing order: {Side} {Quantity} {Symbol}", userId, request.Side, request.Quantity, request.Symbol);
        
        var order = await _tradingService.PlaceOrderAsync(userId, request);
        return Ok(order);
    }

    /// <summary>
    /// Get detailed information about a specific order including trades
    /// </summary>
    /// <param name="id">Order ID</param>
    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrder(ulong id)
    {
        var userId = GetUserId();
        _logger.LogDebug("Fetching order {OrderId} for user {UserId}", id, userId);
        
        try
        {
            var order = await _tradingService.GetOrderAsync(userId, id);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order {OrderId} not found for user {UserId}", id, userId);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get user orders with filtering and pagination
    /// </summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] OrdersQuery query)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid orders query: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(ModelState);
        }
        
        var userId = GetUserId();
        _logger.LogDebug("Fetching orders for user {UserId} with query {Query}", userId, query);
        
        var orders = await _tradingService.GetOrdersAsync(userId, query);
        return Ok(orders);
    }

    /// <summary>
    /// Cancel an existing order
    /// </summary>
    /// <param name="id">Order ID to cancel</param>
    [HttpDelete("orders/{id}")]
    public async Task<IActionResult> CancelOrder(ulong id)
    {
        try
        {
            var userId = GetUserId();
            var order = await _tradingService.CancelOrderAsync(userId, id);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order {OrderId} not found for user {UserId}", id, GetUserId());
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized cancellation attempt for order {OrderId}", id);
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while canceling order {OrderId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling order {OrderId}", id);
            throw;
        }
    }

    /// <summary>
    /// Get trade history with filtering and pagination
    /// </summary>
    [HttpGet("trades")]
    public async Task<IActionResult> GetTrades([FromQuery] TradesQuery query)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid trades query: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(ModelState);
        }
        
        var userId = GetUserId();
        _logger.LogDebug("Fetching trades for user {UserId} with query {Query}", userId, query);
        
        var trades = await _tradingService.GetTradesAsync(userId, query);
        return Ok(trades);
    }

    /// <summary>
    /// Get trade history (alias for /trades endpoint for backward compatibility)
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] TradesQuery query)
    {
        return await GetTrades(query);
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
