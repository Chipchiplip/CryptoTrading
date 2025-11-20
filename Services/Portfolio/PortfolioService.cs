using CryptoTrading.Data;
using CryptoTrading.Interfaces;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Portfolio
{
    /// <summary>
    /// Service for portfolio management and performance tracking
    /// </summary>
    public class PortfolioService : IPortfolioService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICoinGeckoService _coinGeckoService;
        private readonly ILogger<PortfolioService> _logger;

        public PortfolioService(
            ApplicationDbContext context,
            ICoinGeckoService coinGeckoService,
            ILogger<PortfolioService> logger)
        {
            _context = context;
            _coinGeckoService = coinGeckoService;
            _logger = logger;
        }

        /// <summary>
        /// Get complete portfolio overview
        /// </summary>
        public async Task<PortfolioOverviewDto> GetPortfolioOverviewAsync(int userId, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[PortfolioService] GetPortfolioOverviewAsync called for userId={UserId}", userId);
            
            try
            {
                _logger.LogDebug("[PortfolioService] Fetching wallets...");
                // Get all wallets for the user - optimized with AsNoTracking
                var wallets = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Include(w => w.Cryptocurrency)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                
                _logger.LogDebug("[PortfolioService] Found {Count} wallets in {Elapsed}ms", wallets.Count, stopwatch.ElapsedMilliseconds);

                // Get wallet movements to calculate balances - optimized
                var walletIds = wallets.Select(w => w.Id).ToList();
                var movements = await _context.WalletMovements
                    .Where(m => walletIds.Contains(m.WalletId))
                    .GroupBy(m => m.WalletId)
                    .Select(g => new { WalletId = g.Key, Balance = g.Sum(m => m.Amount) })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.WalletId, x => x.Balance, cancellationToken);
                
                _logger.LogDebug("[PortfolioService] Loaded wallet movements in {Elapsed}ms", stopwatch.ElapsedMilliseconds);

                // Get current crypto prices with timeout handling
                _logger.LogDebug("[PortfolioService] Fetching market data from CoinGecko...");
                var marketData = await _coinGeckoService.GetMarketDataAsync();
                _logger.LogDebug("[PortfolioService] Received {Count} market data items in {Elapsed}ms", marketData.Count, stopwatch.ElapsedMilliseconds);
                
                var cryptoPriceMap = marketData.ToDictionary(
                    c => c.Symbol.ToUpper(),
                    c => c.CurrentPrice ?? 0m,
                    StringComparer.OrdinalIgnoreCase);

                // Get all trades for cost basis calculation - optimized with AsNoTracking
                var allTrades = await _context.Trades
                    .Where(t => t.Order.UserId == userId && t.Order.Status == "FILLED")
                    .Include(t => t.Order)
                    .Include(t => t.Cryptocurrency)
                    .AsNoTracking()
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync(cancellationToken);
                
                _logger.LogDebug("[PortfolioService] Loaded {Count} trades in {Elapsed}ms", allTrades.Count, stopwatch.ElapsedMilliseconds);

                var (costStates, realizedPnL) = AnalyzeTrades(allTrades);

                // Calculate holdings with cost basis
                // Group by symbol to handle multiple wallets for same crypto
                var holdingsBySymbol = new Dictionary<string, PortfolioHoldingDto>();
                decimal totalValue = 0m;
                decimal totalCost = 0m;

                // Pre-calculate cost basis for all symbols using in-memory calculation to avoid N+1 queries
                var symbols = wallets
                    .Where(w => w.AssetType == "COIN" && w.Cryptocurrency != null)
                    .Select(w => w.Cryptocurrency!.Symbol.ToUpper())
                    .Distinct()
                    .ToList();
                
                var costBasisMap = new Dictionary<string, decimal>();
                var now = DateTime.UtcNow;
                foreach (var symbol in symbols)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Use in-memory calculation instead of database query
                    costBasisMap[symbol] = CalculateCostBasisInMemory(allTrades, symbol, now);
                }
                
                _logger.LogDebug("[PortfolioService] Calculated cost basis for {Count} symbols in {Elapsed}ms", symbols.Count, stopwatch.ElapsedMilliseconds);

                foreach (var wallet in wallets.Where(w => w.AssetType == "COIN" && w.Cryptocurrency != null))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var balance = movements.GetValueOrDefault(wallet.Id, 0m);
                    // Skip empty wallets or very small balances (rounding errors)
                    if (balance <= 0 || Math.Abs(balance) < 0.00000001m) continue;

                    var symbol = wallet.Cryptocurrency!.Symbol.ToUpper();
                    
                    // If we already have this symbol, aggregate the balance
                    if (holdingsBySymbol.ContainsKey(symbol))
                    {
                        holdingsBySymbol[symbol].Amount += balance;
                    }
                    else
                    {
                        var currentPrice = cryptoPriceMap.GetValueOrDefault(symbol, 0m);
                        
                        // Get image URL from market data or database
                        var cryptoData = marketData.FirstOrDefault(c => c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
                        var imageUrl = cryptoData?.Image ?? wallet.Cryptocurrency.IconUrl;

                        // Use pre-calculated cost basis from costBasisMap, fallback to costStates if needed
                        var avgPrice = costBasisMap.GetValueOrDefault(symbol, 0m);
                        if (avgPrice == 0m && costStates.TryGetValue(symbol, out var costState) && costState.TotalQuantity > 0)
                        {
                            avgPrice = costState.TotalCost / costState.TotalQuantity;
                        }
                        
                        holdingsBySymbol[symbol] = new PortfolioHoldingDto
                        {
                            Symbol = symbol,
                            Name = wallet.Cryptocurrency.Name ?? symbol,
                            Amount = balance,
                            AvgPrice = avgPrice,
                            CurrentPrice = currentPrice,
                            Value = 0, // Will calculate after
                            Pnl = 0, // Will calculate after
                            PnlPercent = 0, // Will calculate after
                            Cost = 0, // Will calculate after
                            ImageUrl = imageUrl
                        };
                    }
                }

                // Calculate final values for each holding
                var holdings = new List<PortfolioHoldingDto>();
                foreach (var holding in holdingsBySymbol.Values)
                {
                    // Skip holdings with zero or very small balance (rounding errors)
                    if (holding.Amount <= 0 || Math.Abs(holding.Amount) < 0.00000001m) continue;

                    // Recalculate cost and value with aggregated amount
                    var cost = holding.Amount * holding.AvgPrice;
                    var value = holding.Amount * holding.CurrentPrice;
                    var pnl = value - cost;
                    var pnlPercent = cost > 0 ? (pnl / cost) * 100 : 0m;

                    holding.Cost = cost;
                    holding.Value = value;
                    holding.Pnl = pnl;
                    holding.PnlPercent = pnlPercent;

                    holdings.Add(holding);

                    totalValue += value;
                    totalCost += cost;
                }

                // Calculate allocations
                foreach (var holding in holdings)
                {
                    holding.Allocation = totalValue > 0 ? (holding.Value / totalValue) * 100 : 0m;
                }

                // Realized PnL is already calculated from AnalyzeTrades above
                // Calculate unrealized PnL
                var unrealizedPnL = totalValue - totalCost;
                var unrealizedPnLPercent = totalCost > 0 ? (unrealizedPnL / totalCost) * 100 : 0m;

                // Get NAV history (last 30 days) - simplified to avoid heavy computation
                _logger.LogDebug("[PortfolioService] Fetching NAV history...");
                var navHistory = await GetNavHistoryAsync(userId, 30, cancellationToken);
                _logger.LogDebug("[PortfolioService] Loaded NAV history in {Elapsed}ms", stopwatch.ElapsedMilliseconds);

                stopwatch.Stop();
                _logger.LogInformation("[PortfolioService] Portfolio overview completed for userId={UserId}, totalValue={TotalValue}, holdings={HoldingCount} in {Elapsed}ms", 
                    userId, totalValue, holdings.Count, stopwatch.ElapsedMilliseconds);

                return new PortfolioOverviewDto
                {
                    TotalValue = totalValue,
                    TotalCost = totalCost,
                    UnrealizedPnL = unrealizedPnL,
                    UnrealizedPnLPercent = unrealizedPnLPercent,
                    RealizedPnL = realizedPnL,
                    Holdings = holdings.OrderByDescending(h => h.Value).ToList(),
                    NavHistory = navHistory
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting portfolio overview for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get portfolio performance metrics
        /// </summary>
        public async Task<PortfolioPerformanceDto> GetPerformanceAsync(int userId, DateTime from, DateTime to)
        {
            var navHistory = await GetNavHistoryAsync(userId, from, to);
            
            if (navHistory.Count < 2)
            {
                return new PortfolioPerformanceDto
                {
                    From = from,
                    To = to,
                    NavHistory = navHistory,
                    TotalReturnPercent = 0m,
                    BestDayReturn = 0m,
                    WorstDayReturn = 0m
                };
            }

            var firstValue = navHistory.First().Value;
            var lastValue = navHistory.Last().Value;
            var totalReturnPercent = firstValue > 0 ? ((lastValue - firstValue) / firstValue) * 100 : 0m;

            // Calculate daily returns
            var dailyReturns = new List<decimal>();
            for (int i = 1; i < navHistory.Count; i++)
            {
                var prevValue = navHistory[i - 1].Value;
                var currValue = navHistory[i].Value;
                if (prevValue > 0)
                {
                    dailyReturns.Add(((currValue - prevValue) / prevValue) * 100);
                }
            }

            return new PortfolioPerformanceDto
            {
                From = from,
                To = to,
                NavHistory = navHistory,
                TotalReturnPercent = totalReturnPercent,
                BestDayReturn = dailyReturns.Any() ? dailyReturns.Max() : 0m,
                WorstDayReturn = dailyReturns.Any() ? dailyReturns.Min() : 0m
            };
        }

        /// <summary>
        /// Calculate realized PnL using FIFO method
        /// </summary>
        public async Task<decimal> CalculateRealizedPnLAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all filled trades - optimized with AsNoTracking
                var trades = await _context.Trades
                    .Where(t => t.Order.UserId == userId && t.Order.Status == "FILLED")
                    .Include(t => t.Order)
                    .Include(t => t.Cryptocurrency)
                    .AsNoTracking()
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync(cancellationToken);

                if (!trades.Any())
                {
                    return 0m;
                }

                // Use AnalyzeTrades to calculate realized PnL
                var (_, realizedPnL) = AnalyzeTrades(trades);
                return realizedPnL;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating realized PnL for user {UserId}", userId);
                return 0m;
            }
        }

        /// <summary>
        /// Calculate average cost basis for a symbol at a specific point in time using FIFO
        /// </summary>
        public async Task<decimal> GetAverageCostBasisAsync(int userId, string symbol, DateTime asOfDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedSymbol = symbol.ToUpperInvariant();

                var trades = await _context.Trades
                    .Where(t => t.Order.UserId == userId
                        && t.Order.Status == "FILLED"
                        && t.CreatedAt <= asOfDate
                        && t.Cryptocurrency.Symbol.ToUpper() == normalizedSymbol)
                    .Include(t => t.Order)
                    .Include(t => t.Cryptocurrency)
                    .AsNoTracking()
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync(cancellationToken);

                if (trades.Count == 0)
                {
                    return 0m;
                }

                var (states, _) = AnalyzeTrades(trades);

                if (states.TryGetValue(normalizedSymbol, out var state) && state.TotalQuantity > 0)
                {
                    return state.TotalCost / state.TotalQuantity;
                }

                return 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating cost basis for symbol {Symbol} at {Date}", symbol, asOfDate);
                return 0m;
            }
        }

        private (Dictionary<string, CostBasisState> States, decimal RealizedPnL) AnalyzeTrades(IEnumerable<Trade> trades)
        {
            var states = new Dictionary<string, CostBasisState>(StringComparer.OrdinalIgnoreCase);
            decimal realizedPnL = 0m;

            var orderedTrades = trades is IList<Trade> list ? list : trades.OrderBy(t => t.CreatedAt).ToList();

            foreach (var trade in orderedTrades)
            {
                if (trade.Order == null || trade.Cryptocurrency == null) continue;

                var symbol = trade.Cryptocurrency.Symbol?.ToUpperInvariant();
                if (string.IsNullOrEmpty(symbol)) continue;

                if (!states.TryGetValue(symbol, out var state))
                {
                    state = new CostBasisState();
                    states[symbol] = state;
                }

                var side = trade.Order.Side?.ToUpperInvariant();
                if (side == "BUY")
                {
                    state.AddLot(trade.QuantityCoin, trade.PriceUsd, trade.FeeUsd);
                }
                else if (side == "SELL")
                {
                    var remaining = state.Consume(trade.QuantityCoin, trade.PriceUsd, ref realizedPnL);
                    if (remaining > 0)
                    {
                        // Selling more than existing holdings – treat remainder as zero cost basis
                        realizedPnL += trade.PriceUsd * remaining;
                    }

                    realizedPnL -= trade.FeeUsd;
                }
            }

            return (states, realizedPnL);
        }

        private sealed class CostBasisState
        {
            private readonly Queue<CostBasisLot> _lots = new();

            public decimal TotalQuantity { get; private set; }
            public decimal TotalCost { get; private set; }

            public void AddLot(decimal quantity, decimal priceUsd, decimal feeUsd)
            {
                if (quantity <= 0) return;
                var totalCost = priceUsd * quantity + feeUsd;
                var costPerUnit = totalCost / quantity;
                _lots.Enqueue(new CostBasisLot(quantity, costPerUnit));
                TotalQuantity += quantity;
                TotalCost += totalCost;
            }

            public decimal Consume(decimal quantity, decimal sellPrice, ref decimal realizedPnL)
            {
                var remaining = quantity;
                if (remaining <= 0) return 0m;

                while (remaining > 0 && _lots.Count > 0)
                {
                    var lot = _lots.Peek();
                    var qtyUsed = Math.Min(lot.Quantity, remaining);
                    realizedPnL += (sellPrice - lot.CostPerUnit) * qtyUsed;

                    lot.Quantity -= qtyUsed;
                    TotalQuantity -= qtyUsed;
                    TotalCost -= qtyUsed * lot.CostPerUnit;
                    remaining -= qtyUsed;

                    if (lot.Quantity <= 0.00000001m)
                    {
                        _lots.Dequeue();
                    }
                }

                return remaining;
            }
        }

        private sealed class CostBasisLot
        {
            public CostBasisLot(decimal quantity, decimal costPerUnit)
            {
                Quantity = quantity;
                CostPerUnit = costPerUnit;
            }

            public decimal Quantity { get; set; }
            public decimal CostPerUnit { get; }
        }

        /// <summary>
        /// Calculate cost basis in memory from pre-loaded trades (optimized version to avoid N+1 queries)
        /// </summary>
        private decimal CalculateCostBasisInMemory(List<Models.Trade> allTrades, string symbol, DateTime asOfDate)
        {
            try
            {
                // Filter trades for this symbol and date
                var buyTrades = allTrades
                    .Where(t => t.Cryptocurrency.Symbol.ToUpper() == symbol.ToUpper()
                        && t.Order.Side == "BUY"
                        && t.CreatedAt <= asOfDate)
                    .OrderBy(t => t.CreatedAt)
                    .ToList();

                var sellTrades = allTrades
                    .Where(t => t.Cryptocurrency.Symbol.ToUpper() == symbol.ToUpper()
                        && t.Order.Side == "SELL"
                        && t.CreatedAt <= asOfDate)
                    .OrderBy(t => t.CreatedAt)
                    .ToList();

                // Calculate net holdings using FIFO
                decimal totalCost = 0m;
                decimal totalQuantity = 0m;
                var fifoQueue = new Queue<(decimal Quantity, decimal CostPerUnit)>();

                // Process BUY trades (add to FIFO queue)
                foreach (var buyTrade in buyTrades)
                {
                    var costPerUnit = (buyTrade.PriceUsd * buyTrade.QuantityCoin + buyTrade.FeeUsd) / buyTrade.QuantityCoin;
                    fifoQueue.Enqueue((buyTrade.QuantityCoin, costPerUnit));
                    totalCost += buyTrade.PriceUsd * buyTrade.QuantityCoin + buyTrade.FeeUsd;
                    totalQuantity += buyTrade.QuantityCoin;
                }

                // Process SELL trades (remove from FIFO queue)
                foreach (var sellTrade in sellTrades)
                {
                    var remainingToSell = sellTrade.QuantityCoin;
                    while (remainingToSell > 0 && fifoQueue.Count > 0)
                    {
                        var (quantity, costPerUnit) = fifoQueue.Dequeue();
                        if (quantity <= remainingToSell)
                        {
                            // Remove entire lot
                            totalCost -= quantity * costPerUnit;
                            totalQuantity -= quantity;
                            remainingToSell -= quantity;
                        }
                        else
                        {
                            // Partial removal
                            totalCost -= remainingToSell * costPerUnit;
                            totalQuantity -= remainingToSell;
                            fifoQueue.Enqueue((quantity - remainingToSell, costPerUnit));
                            remainingToSell = 0;
                        }
                    }
                }

                // Calculate weighted average cost basis
                return totalQuantity > 0 ? totalCost / totalQuantity : 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating cost basis in memory for symbol {Symbol} at {Date}", symbol, asOfDate);
                return 0m;
            }
        }

        /// <summary>
        /// Get NAV history for the last N days
        /// </summary>
        private async Task<List<NavDataPoint>> GetNavHistoryAsync(int userId, int days, CancellationToken cancellationToken = default)
        {
            var to = DateTime.UtcNow.Date;
            var from = to.AddDays(-days);
            return await GetNavHistoryAsync(userId, from, to, cancellationToken);
        }

        /// <summary>
        /// Get NAV history for a date range - simplified version for performance
        /// </summary>
        private async Task<List<NavDataPoint>> GetNavHistoryAsync(int userId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            try
            {
                // Simplified NAV history - just return current value for all dates
                // This avoids heavy computation that causes timeout
                var navHistory = new List<NavDataPoint>();
                var currentDate = from.Date;

                // Calculate current portfolio value - optimized
                var wallets = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Include(w => w.Cryptocurrency)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var walletIds = wallets.Select(w => w.Id).ToList();
                var movements = await _context.WalletMovements
                    .Where(m => walletIds.Contains(m.WalletId))
                    .GroupBy(m => m.WalletId)
                    .Select(g => new { WalletId = g.Key, Balance = g.Sum(m => m.Amount) })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.WalletId, x => x.Balance, cancellationToken);

                // Use cached market data if available, otherwise fetch
                var marketData = await _coinGeckoService.GetMarketDataAsync();
                var cryptoPriceMap = marketData.ToDictionary(
                    c => c.Symbol.ToUpper(),
                    c => c.CurrentPrice ?? 0m,
                    StringComparer.OrdinalIgnoreCase);

                // Calculate current portfolio value (crypto + USD)
                decimal currentValue = 0m;
                
                // Add USD balance
                var usdWallet = wallets.FirstOrDefault(w => w.AssetType == "FIAT" && w.CurrencyCode == "USD");
                if (usdWallet != null)
                {
                    currentValue += movements.GetValueOrDefault(usdWallet.Id, 0m);
                }
                
                // Add crypto values
                foreach (var wallet in wallets.Where(w => w.AssetType == "COIN" && w.Cryptocurrency != null))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var balance = movements.GetValueOrDefault(wallet.Id, 0m);
                    if (balance <= 0) continue;
                    var symbol = wallet.Cryptocurrency!.Symbol.ToUpper();
                    var currentPrice = cryptoPriceMap.GetValueOrDefault(symbol, 0m);
                    currentValue += balance * currentPrice;
                }

                // Generate daily NAV points (simplified - use current value for all dates to avoid timeout)
                // In production, implement actual historical NAV tracking with daily snapshots
                while (currentDate <= to.Date)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    navHistory.Add(new NavDataPoint
                    {
                        Date = currentDate,
                        Value = currentValue
                    });
                    currentDate = currentDate.AddDays(1);
                }

                return navHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting NAV history for user {UserId}", userId);
                return new List<NavDataPoint>();
            }
        }
    }
}

