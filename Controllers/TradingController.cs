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
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        _logger.LogDebug("Fetching dashboard for user {UserId}", userId);
        
        var summary = await GetDashboardSummaryData(userId, cancellationToken);
        var navHistory = await GetDashboardNavHistoryData(userId, DateTime.UtcNow.AddDays(-29).Date, cancellationToken);
        var pnlHistory = await GetDashboardPnlHistoryData(userId, "hourly", DateTime.UtcNow.Date, cancellationToken);
        
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
    public async Task<IActionResult> GetDashboardSummaryEndpoint(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogDebug("Fetching dashboard summary for user {UserId}", userId);
            
            var summary = await GetDashboardSummaryData(userId, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard summary for user");
            return StatusCode(500, new { message = "An error occurred while fetching dashboard summary", error = ex.Message });
        }
    }

    /// <summary>
    /// Get NAV history for the last 30 days
    /// </summary>
    [HttpGet("dashboard/nav")]
    public async Task<IActionResult> GetDashboardNav([FromQuery] string? from, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogDebug("Fetching NAV history for user {UserId}", userId);
            
            // Validate date parameter
            DateTime fromDate;
            if (string.IsNullOrEmpty(from))
            {
                fromDate = DateTime.UtcNow.AddDays(-29).Date;
            }
            else
            {
                if (!DateTime.TryParse(from, out fromDate))
                {
                    _logger.LogWarning("Invalid date format for 'from' parameter: {From}", from);
                    return BadRequest(new { message = $"Invalid date format for 'from' parameter. Expected format: YYYY-MM-DD, received: {from}" });
                }
                fromDate = fromDate.Date;
            }
            
            var navHistory = await GetDashboardNavHistoryData(userId, fromDate, cancellationToken);
            return Ok(navHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching NAV history for user");
            return StatusCode(500, new { message = "An error occurred while fetching NAV history", error = ex.Message });
        }
    }

    /// <summary>
    /// Get PnL history for a specific date
    /// </summary>
    [HttpGet("dashboard/pnl")]
    public async Task<IActionResult> GetDashboardPnl([FromQuery] string granularity = "hourly", [FromQuery] string? date = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogDebug("Fetching PnL history for user {UserId}", userId);
            
            // Validate date parameter
            DateTime targetDate;
            if (string.IsNullOrEmpty(date))
            {
                targetDate = DateTime.UtcNow.Date;
            }
            else
            {
                if (!DateTime.TryParse(date, out targetDate))
                {
                    _logger.LogWarning("Invalid date format for 'date' parameter: {Date}", date);
                    return BadRequest(new { message = $"Invalid date format for 'date' parameter. Expected format: YYYY-MM-DD, received: {date}" });
                }
                targetDate = targetDate.Date;
            }
            
            // Validate granularity
            if (granularity != "hourly" && granularity != "daily" && granularity != "weekly")
            {
                _logger.LogWarning("Invalid granularity parameter: {Granularity}", granularity);
                return BadRequest(new { message = $"Invalid granularity parameter. Expected: hourly, daily, or weekly, received: {granularity}" });
            }
            
            var pnlHistory = await GetDashboardPnlHistoryData(userId, granularity, targetDate, cancellationToken);
            return Ok(pnlHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching PnL history for user");
            return StatusCode(500, new { message = "An error occurred while fetching PnL history", error = ex.Message });
        }
    }

    // Helper methods
    private async Task<DashboardSummaryDto> GetDashboardSummaryData(int userId, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("[Dashboard Summary] Starting for user {UserId}", userId);
        
        // Get wallets and calculate total balance
        var wallets = await _db.Wallets
            .Where(w => w.UserId == userId)
            .Include(w => w.Cryptocurrency)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        _logger.LogDebug("[Dashboard Summary] Loaded {Count} wallets in {Elapsed}ms", wallets.Count, stopwatch.ElapsedMilliseconds);

        var walletIds = wallets.Select(w => w.Id).ToList();
        var movements = await _db.WalletMovements
            .Where(m => walletIds.Contains(m.WalletId))
            .GroupBy(m => m.WalletId)
            .Select(g => new { WalletId = g.Key, Balance = g.Sum(m => m.Amount) })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.WalletId, x => x.Balance, cancellationToken);
        
        _logger.LogDebug("[Dashboard Summary] Loaded wallet movements in {Elapsed}ms", stopwatch.ElapsedMilliseconds);

        var marketData = await _coinGeckoService.GetMarketDataAsync();
        
        _logger.LogDebug("[Dashboard Summary] Loaded market data in {Elapsed}ms", stopwatch.ElapsedMilliseconds);
        
        // Get locked balances from active order holds
        var orderHolds = await _db.OrderHolds
            .Where(h => walletIds.Contains(h.WalletId) && h.ReleasedAt == null)
            .GroupBy(h => h.WalletId)
            .Select(g => new { WalletId = g.Key, LockedAmount = g.Sum(h => h.Amount) })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.WalletId, x => x.LockedAmount, cancellationToken);
        
        _logger.LogDebug("[Dashboard Summary] Loaded order holds in {Elapsed}ms", stopwatch.ElapsedMilliseconds);
        
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
                .AsNoTracking()
                .SumAsync(m => m.Amount, cancellationToken);
            previousTotalBalance = usdMovementsYesterday;
        }
        
        _logger.LogDebug("[Dashboard Summary] Calculated yesterday balance in {Elapsed}ms", stopwatch.ElapsedMilliseconds);

        // Get positions (net quantity per cryptocurrency) as of yesterday
        var allTrades = await _db.Trades
            .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId && t.CreatedAt.Date <= yesterday))
            .Include(t => t.Order)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        _logger.LogDebug("[Dashboard Summary] Loaded trades in {Elapsed}ms", stopwatch.ElapsedMilliseconds);

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
                .AsNoTracking()
                .ToDictionaryAsync(x => x.CryptocurrencyId, x => x.Price, cancellationToken);
            
            _logger.LogDebug("[Dashboard Summary] Loaded yesterday prices in {Elapsed}ms", stopwatch.ElapsedMilliseconds);
            
            // Fallback: if no historical price before/at yesterday, try latest known price overall
            var missingPriceIds = positions
                .Where(p => !yesterdayPrices.ContainsKey(p.CryptocurrencyId) || yesterdayPrices[p.CryptocurrencyId] <= 0)
                .Select(p => p.CryptocurrencyId)
                .Distinct()
                .ToList();

            if (missingPriceIds.Any())
            {
                var fallbackPrices = await _db.CryptoPrices
                    .Where(p => missingPriceIds.Contains(p.CryptocurrencyId) && p.CollectedAtUtc <= DateTime.UtcNow)
                    .GroupBy(p => p.CryptocurrencyId)
                    .Select(g => new
                    {
                        CryptocurrencyId = g.Key,
                        Price = g.OrderByDescending(p => p.CollectedAtUtc).Select(p => p.PriceUsd).FirstOrDefault()
                    })
                    .ToListAsync();

                foreach (var fp in fallbackPrices)
                {
                    if (fp.Price > 0)
                    {
                        yesterdayPrices[fp.CryptocurrencyId] = fp.Price;
                    }
                }
            }
            
            foreach (var pos in positions)
            {
                var latestPrice = yesterdayPrices.GetValueOrDefault(pos.CryptocurrencyId, 0m);
                previousTotalBalance += pos.QtyCoin * (latestPrice > 0 ? latestPrice : 0);
            }
        }

        // Get open orders count
        var openOrders = await _db.Orders
            .Where(o => o.UserId == userId && (o.Status == "NEW" || o.Status == "PARTIAL"))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        _logger.LogDebug("[Dashboard Summary] Loaded open orders in {Elapsed}ms", stopwatch.ElapsedMilliseconds);

        var openOrdersBuy = openOrders.Count(o => o.Side == "BUY");
        var openOrdersSell = openOrders.Count(o => o.Side == "SELL");

        // Calculate today's PnL from actual trades
        var today = DateTime.UtcNow.Date;
        var todayTrades = await _db.Trades
            .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId && t.CreatedAt.Date == today))
            .Include(t => t.Order)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        _logger.LogDebug("[Dashboard Summary] Loaded today trades in {Elapsed}ms", stopwatch.ElapsedMilliseconds);

        var todayPnl = todayTrades.Sum(t => 
        {
            if (t.Order.Side == "SELL")
                return (t.PriceUsd * t.QuantityCoin) - t.FeeUsd;
            else // BUY
                return -((t.PriceUsd * t.QuantityCoin) + t.FeeUsd);
        });

        // Percent PnL should be based on starting equity of the day (yesterday's NAV)
        var pnlDenominator = previousTotalBalance > 0 ? previousTotalBalance : totalBalance;
        var todayPnlPercent = pnlDenominator > 0 ? (todayPnl / pnlDenominator) * 100m : 0m;

        var totalBalanceChange = totalBalance - previousTotalBalance;
        var totalBalanceChangePercent = previousTotalBalance > 0 
            ? (totalBalanceChange / previousTotalBalance) * 100m 
            : 0m;

        stopwatch.Stop();
        _logger.LogInformation("[Dashboard Summary] Completed for user {UserId} in {Elapsed}ms", userId, stopwatch.ElapsedMilliseconds);

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

    private async Task<DashboardNavHistoryDto> GetDashboardNavHistoryData(int userId, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("[Dashboard NAV] Starting for user {UserId}, from {FromDate}", userId, fromDate);
        
        // Limit date range to prevent excessive data loading (max 90 days)
        var maxDaysBack = 90;
        var earliestAllowedDate = DateTime.UtcNow.Date.AddDays(-maxDaysBack);
        if (fromDate < earliestAllowedDate)
        {
            _logger.LogWarning("[Dashboard NAV] Date range too large, limiting to {MaxDays} days", maxDaysBack);
            fromDate = earliestAllowedDate;
        }
        
        var toDate = DateTime.UtcNow.Date;
        var data = new List<DashboardNavDataPointDto>();
        
        // Get all trades up to now - optimized with AsNoTracking and date filter
        var allTrades = await _db.Trades
            .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId) && 
                       t.CreatedAt.Date <= toDate)
            .Include(t => t.Order)
            .Include(t => t.Cryptocurrency)
            .AsNoTracking()
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
        
        _logger.LogDebug("[Dashboard NAV] Loaded {Count} trades in {Elapsed}ms", allTrades.Count, stopwatch.ElapsedMilliseconds);

        // Prepare position map and starting positions before fromDate
        var positions = new Dictionary<int, decimal>();
        foreach (var trade in allTrades.Where(t => t.CreatedAt.Date < fromDate))
        {
            var key = trade.CryptocurrencyId;
            var delta = trade.Order.Side == "BUY" ? trade.QuantityCoin : -trade.QuantityCoin;
            positions[key] = positions.GetValueOrDefault(key) + delta;
        }

        // Collect crypto ids that ever appear (for price preload)
        var cryptoIds = allTrades.Select(t => t.CryptocurrencyId).Distinct().ToList();
        var allPrices = new Dictionary<(int CryptoId, DateTime Date), decimal>();
        
        if (cryptoIds.Any())
        {
            // Include prices up to toDate so we can carry-forward latest known price
            var priceData = await _db.CryptoPrices
                .Where(p => cryptoIds.Contains(p.CryptocurrencyId) && 
                           p.CollectedAtUtc.Date >= fromDate && 
                           p.CollectedAtUtc.Date <= toDate)
                .Select(p => new { p.CryptocurrencyId, Date = p.CollectedAtUtc.Date, p.PriceUsd, p.CollectedAtUtc })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("[Dashboard NAV] Loaded price data in {Elapsed}ms", stopwatch.ElapsedMilliseconds);
            
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

            // Seed a fallback price at fromDate using the latest price before fromDate (if any)
            var preStartPrices = priceData
                .Where(p => p.Date < fromDate)
                .GroupBy(p => p.CryptocurrencyId)
                .Select(g => new
                {
                    CryptocurrencyId = g.Key,
                    Price = g.OrderByDescending(p => p.Date).ThenByDescending(p => p.CollectedAtUtc).First().PriceUsd
                });

            foreach (var price in preStartPrices)
            {
                var key = (price.CryptocurrencyId, fromDate);
                if (!allPrices.ContainsKey(key))
                {
                    allPrices[key] = price.Price;
                }
            }
        }

        // Preload USD wallet data ONCE (not in the loop!)
        var usdWallets = await _db.Wallets
            .Where(w => w.UserId == userId && w.AssetType == "FIAT" && w.CurrencyCode == "USD")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var usdWalletIds = usdWallets.Select(w => w.Id).ToList();
        
        // Preload ALL USD movements ONCE - limit to date range
        var usdMovements = new List<(DateTime Date, decimal Amount)>();
        if (usdWalletIds.Any())
        {
            var movementsData = await _db.WalletMovements
                .Where(m => usdWalletIds.Contains(m.WalletId) && m.CreatedAt.Date <= toDate)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new { Date = m.CreatedAt.Date, m.Amount })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            
            usdMovements = movementsData.Select(x => (x.Date, x.Amount)).ToList();
            _logger.LogDebug("[Dashboard NAV] Loaded USD movements in {Elapsed}ms", stopwatch.ElapsedMilliseconds);
        }

        // Calculate cumulative USD balance by date
        var usdBalanceByDate = new Dictionary<DateTime, decimal>();
        decimal cumulativeUsd = 0m;
        foreach (var movement in usdMovements)
        {
            cumulativeUsd += movement.Amount;
            usdBalanceByDate[movement.Date] = cumulativeUsd;
        }

        // Iterate FORWARD from fromDate to toDate to correctly track position changes
        var daysToCalculate = (toDate - fromDate).Days + 1;
        
        // We already have 'positions' initialized with holdings BEFORE fromDate.
        // We need to update this state day by day.
        
        for (int i = 0; i < daysToCalculate; i++)
        {
            var date = fromDate.AddDays(i);
            
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Update positions with trades that happened ON this date
            var tradesOnDate = allTrades.Where(t => t.CreatedAt.Date == date).ToList();
            foreach (var trade in tradesOnDate)
            {
                var key = trade.CryptocurrencyId;
                var delta = trade.Order.Side == "BUY" ? trade.QuantityCoin : -trade.QuantityCoin;
                positions[key] = positions.GetValueOrDefault(key) + delta;
            }
            
            decimal nav = 0m;

            // 2. Value current positions using latest price on or before current date
            foreach (var pos in positions)
            {
                if (pos.Value == 0) continue; // Skip empty positions

                decimal latestPrice = 0m;
                // Find price for this specific date
                if (allPrices.TryGetValue((pos.Key, date), out var price))
                {
                    latestPrice = price;
                }
                else
                {
                    // Fallback: find latest price before this date
                    // Since we don't have a time-series structure for quick lookup, we search backwards
                    // Optimization: In a real app, we'd use a better data structure. 
                    // For now, limit lookback to avoid O(N^2) in worst case
                    for (var checkDate = date.AddDays(-1); checkDate >= fromDate.AddDays(-5); checkDate = checkDate.AddDays(-1))
                    {
                        if (allPrices.TryGetValue((pos.Key, checkDate), out var p))
                        {
                            latestPrice = p;
                            break;
                        }
                    }
                }

                nav += pos.Value * latestPrice;
            }

            // 3. Add USD balance
            if (usdBalanceByDate.Any())
            {
                // Find the latest USD balance on or before this date
                // Optimization: Since we iterate forward, we could cache the last known balance
                var usdBalanceObj = usdBalanceByDate
                    .Where(kvp => kvp.Key <= date)
                    .OrderByDescending(kvp => kvp.Key)
                    .Select(x => (decimal?)x.Value)
                    .FirstOrDefault();
                
                if (usdBalanceObj.HasValue)
                {
                    nav += usdBalanceObj.Value;
                }
            }

            data.Add(new DashboardNavDataPointDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Value = Math.Round(nav, 2)
            });
        }

        stopwatch.Stop();
        _logger.LogInformation("[Dashboard NAV] Completed for user {UserId} in {Elapsed}ms, returned {Count} data points", 
            userId, stopwatch.ElapsedMilliseconds, data.Count);

        return new DashboardNavHistoryDto
        {
            From = fromDate.ToString("yyyy-MM-dd"),
            To = toDate.ToString("yyyy-MM-dd"),
            Data = data
        };
    }

    private async Task<DashboardPnlHistoryDto> GetDashboardPnlHistoryData(int userId, string granularity, DateTime targetDate, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("[Dashboard PnL] Starting for user {UserId}, granularity {Granularity}, date {Date}", 
            userId, granularity, targetDate);

        var data = new List<DashboardPnlDataPointDto>();

        if (granularity == "hourly")
        {
            // Get all trades for the target date - optimized with AsNoTracking
            var dayTrades = await _db.Trades
                .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId) && 
                           t.CreatedAt.Date == targetDate)
                .Include(t => t.Order)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("[Dashboard PnL] Loaded {Count} trades for date {Date} in {Elapsed}ms", 
                dayTrades.Count, targetDate, stopwatch.ElapsedMilliseconds);

            // Group by hour and calculate PnL
            for (int hour = 0; hour < 24; hour++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
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
        else if (granularity == "daily")
        {
            // For daily granularity, get trades for the week containing targetDate
            var weekStart = targetDate.AddDays(-(int)targetDate.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);
            
            var weekTrades = await _db.Trades
                .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId) && 
                           t.CreatedAt.Date >= weekStart && t.CreatedAt.Date < weekEnd)
                .Include(t => t.Order)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            
            // Group by day
            for (int day = 0; day < 7; day++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var dayStart = weekStart.AddDays(day);
                var dayEnd = dayStart.AddDays(1);
                
                var dayTrades = weekTrades
                    .Where(t => t.CreatedAt >= dayStart && t.CreatedAt < dayEnd)
                    .ToList();
                
                var pnl = dayTrades.Sum(t =>
                {
                    if (t.Order.Side == "SELL")
                        return (t.PriceUsd * t.QuantityCoin) - t.FeeUsd;
                    else // BUY
                        return -((t.PriceUsd * t.QuantityCoin) + t.FeeUsd);
                });
                
                data.Add(new DashboardPnlDataPointDto
                {
                    Time = dayStart.ToString("yyyy-MM-dd"),
                    Pnl = Math.Round(pnl, 2)
                });
            }
        }
        else if (granularity == "weekly")
        {
            // For weekly granularity, get trades for the last 4 weeks
            var fourWeeksAgo = targetDate.AddDays(-28);
            
            var monthTrades = await _db.Trades
                .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId) && 
                           t.CreatedAt.Date >= fourWeeksAgo && t.CreatedAt.Date <= targetDate)
                .Include(t => t.Order)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            
            // Group by week
            for (int week = 0; week < 4; week++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var weekStart = fourWeeksAgo.AddDays(week * 7);
                var weekEnd = weekStart.AddDays(7);
                
                var weekTrades = monthTrades
                    .Where(t => t.CreatedAt >= weekStart && t.CreatedAt < weekEnd)
                    .ToList();
                
                var pnl = weekTrades.Sum(t =>
                {
                    if (t.Order.Side == "SELL")
                        return (t.PriceUsd * t.QuantityCoin) - t.FeeUsd;
                    else // BUY
                        return -((t.PriceUsd * t.QuantityCoin) + t.FeeUsd);
                });
                
                data.Add(new DashboardPnlDataPointDto
                {
                    Time = $"Week {week + 1}",
                    Pnl = Math.Round(pnl, 2)
                });
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("[Dashboard PnL] Completed for user {UserId} in {Elapsed}ms, returned {Count} data points", 
            userId, stopwatch.ElapsedMilliseconds, data.Count);

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
