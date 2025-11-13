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
        public async Task<PortfolioOverviewDto> GetPortfolioOverviewAsync(int userId)
        {
            try
            {
                // Get all wallets for the user
                var wallets = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Include(w => w.Cryptocurrency)
                    .ToListAsync();

                // Get wallet movements to calculate balances
                var walletIds = wallets.Select(w => w.Id).ToList();
                var movements = await _context.WalletMovements
                    .Where(m => walletIds.Contains(m.WalletId))
                    .GroupBy(m => m.WalletId)
                    .Select(g => new { WalletId = g.Key, Balance = g.Sum(m => m.Amount) })
                    .ToDictionaryAsync(x => x.WalletId, x => x.Balance);

                // Get current crypto prices
                var marketData = await _coinGeckoService.GetMarketDataAsync();
                var cryptoPriceMap = marketData.ToDictionary(
                    c => c.Symbol.ToUpper(),
                    c => c.CurrentPrice ?? 0m,
                    StringComparer.OrdinalIgnoreCase);

                // Get all trades for cost basis calculation
                var allTrades = await _context.Trades
                    .Where(t => t.Order.UserId == userId && t.Order.Status == "FILLED")
                    .Include(t => t.Order)
                    .Include(t => t.Cryptocurrency)
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync();

                var (costStates, realizedPnL) = AnalyzeTrades(allTrades);

                // Calculate holdings with cost basis
                // Group by symbol to handle multiple wallets for same crypto
                var holdingsBySymbol = new Dictionary<string, PortfolioHoldingDto>();
                decimal totalValue = 0m;
                decimal totalCost = 0m;

                foreach (var wallet in wallets.Where(w => w.AssetType == "COIN" && w.Cryptocurrency != null))
                {
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

                        // Calculate average cost basis using the precomputed trade snapshot
                        var avgPrice = 0m;
                        if (costStates.TryGetValue(symbol, out var costState) && costState.TotalQuantity > 0)
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

                // Calculate unrealized PnL
                var unrealizedPnL = totalValue - totalCost;
                var unrealizedPnLPercent = totalCost > 0 ? (unrealizedPnL / totalCost) * 100 : 0m;

                // Get NAV history (last 30 days)
                var navHistory = await GetNavHistoryAsync(userId, 30);

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
        public async Task<decimal> CalculateRealizedPnLAsync(int userId)
        {
            try
            {
                var trades = await _context.Trades
                    .Where(t => t.Order.UserId == userId && t.Order.Status == "FILLED")
                    .Include(t => t.Order)
                    .Include(t => t.Cryptocurrency)
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync();

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
        public async Task<decimal> GetAverageCostBasisAsync(int userId, string symbol, DateTime asOfDate)
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
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync();

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
        /// Get NAV history for the last N days
        /// </summary>
        private async Task<List<NavDataPoint>> GetNavHistoryAsync(int userId, int days)
        {
            var to = DateTime.UtcNow.Date;
            var from = to.AddDays(-days);
            return await GetNavHistoryAsync(userId, from, to);
        }

        /// <summary>
        /// Get NAV history for a date range
        /// </summary>
        private async Task<List<NavDataPoint>> GetNavHistoryAsync(int userId, DateTime from, DateTime to)
        {
            try
            {
                // For now, return simplified NAV history
                // In production, you might want to store daily NAV snapshots
                var navHistory = new List<NavDataPoint>();
                var currentDate = from.Date;

                // Calculate current portfolio value without calling GetPortfolioOverviewAsync to avoid recursion
                var wallets = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Include(w => w.Cryptocurrency)
                    .ToListAsync();

                var walletIds = wallets.Select(w => w.Id).ToList();
                var movements = await _context.WalletMovements
                    .Where(m => walletIds.Contains(m.WalletId))
                    .GroupBy(m => m.WalletId)
                    .Select(g => new { WalletId = g.Key, Balance = g.Sum(m => m.Amount) })
                    .ToDictionaryAsync(x => x.WalletId, x => x.Balance);

                var marketData = await _coinGeckoService.GetMarketDataAsync();
                var cryptoPriceMap = marketData.ToDictionary(
                    c => c.Symbol.ToUpper(),
                    c => c.CurrentPrice ?? 0m,
                    StringComparer.OrdinalIgnoreCase);

                decimal currentValue = 0m;
                foreach (var wallet in wallets.Where(w => w.AssetType == "COIN" && w.Cryptocurrency != null))
                {
                    var balance = movements.GetValueOrDefault(wallet.Id, 0m);
                    if (balance <= 0) continue;
                    var symbol = wallet.Cryptocurrency!.Symbol.ToUpper();
                    var currentPrice = cryptoPriceMap.GetValueOrDefault(symbol, 0m);
                    currentValue += balance * currentPrice;
                }

                // Generate daily NAV points (simplified - in production, use actual historical data)
                while (currentDate <= to.Date)
                {
                    // For now, use current value for all dates
                    // TODO: Implement actual historical NAV tracking
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

