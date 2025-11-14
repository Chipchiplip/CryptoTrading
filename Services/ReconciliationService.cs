using CryptoTrading.Data;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CryptoTrading.Services
{
    public interface IReconciliationService
    {
        Task<ReconciliationResult> ReconcileOrdersAndTradesAsync();
        Task<ReconciliationResult> ReconcileWalletBalancesAsync();
        Task<List<ReconciliationResult>> GetRecentResultsAsync(int count = 10);
    }

    public class ReconciliationService : IReconciliationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReconciliationService> _logger;

        public ReconciliationService(
            ApplicationDbContext context,
            ILogger<ReconciliationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReconciliationResult> ReconcileOrdersAndTradesAsync()
        {
            var startTime = DateTime.UtcNow;
            var mismatches = new List<object>();
            int totalChecked = 0;

            try
            {
                _logger.LogInformation("Starting order-trade reconciliation");

                // Get all filled orders
                var filledOrders = await _context.Orders
                    .Where(o => o.Status == "FILLED")
                    .Include(o => o.Cryptocurrency)
                    .AsNoTracking()
                    .ToListAsync();

                totalChecked = filledOrders.Count;

                foreach (var order in filledOrders)
                {
                    // Get trades for this order
                    var trades = await _context.Trades
                        .Where(t => t.OrderId == order.Id)
                        .AsNoTracking()
                        .ToListAsync();

                    // Check 1: Total trade quantity should match filled quantity
                    var totalTradeQty = trades.Sum(t => t.QuantityCoin);
                    if (Math.Abs(totalTradeQty - order.FilledQty) > 0.00000001m)
                    {
                        mismatches.Add(new
                        {
                            Type = "QUANTITY_MISMATCH",
                            OrderId = order.Id,
                            Symbol = order.Cryptocurrency.Symbol,
                            OrderFilledQty = order.FilledQty,
                            TradesQty = totalTradeQty,
                            Difference = totalTradeQty - order.FilledQty
                        });
                        _logger.LogWarning("Order {OrderId}: Quantity mismatch - Order: {OrderQty}, Trades: {TradeQty}",
                            order.Id, order.FilledQty, totalTradeQty);
                    }

                    // Check 2: Wallet movements should exist for filled orders
                    var userId = order.UserId;
                    var wallet = order.Side == "BUY"
                        ? await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId && w.CryptocurrencyId == order.CryptocurrencyId)
                        : await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId && w.AssetType == "FIAT" && w.CurrencyCode == "USD");

                    if (wallet != null)
                    {
                        var movements = await _context.WalletMovements
                            .Where(m => m.WalletId == wallet.Id && m.RefType == "TRADE")
                            .AsNoTracking()
                            .ToListAsync();

                        if (!movements.Any() && order.FilledQty > 0)
                        {
                            mismatches.Add(new
                            {
                                Type = "MISSING_WALLET_MOVEMENT",
                                OrderId = order.Id,
                                Symbol = order.Cryptocurrency.Symbol,
                                UserId = userId
                            });
                        }
                    }

                    // Check 3: OrderHolds should be released for filled orders
                    var unreleasedHolds = await _context.OrderHolds
                        .Where(h => h.OrderId == order.Id && h.ReleasedAt == null)
                        .AsNoTracking()
                        .ToListAsync();

                    if (unreleasedHolds.Any())
                    {
                        mismatches.Add(new
                        {
                            Type = "UNRELEASED_HOLD",
                            OrderId = order.Id,
                            Symbol = order.Cryptocurrency.Symbol,
                            UnreleasedAmount = unreleasedHolds.Sum(h => h.Amount)
                        });
                        _logger.LogWarning("Order {OrderId}: Has unreleased holds totaling {Amount}",
                            order.Id, unreleasedHolds.Sum(h => h.Amount));
                    }
                }

                var result = new ReconciliationResult
                {
                    ReconciliationTime = startTime,
                    EntityType = "ORDERS_TRADES",
                    TotalChecked = totalChecked,
                    MismatchCount = mismatches.Count,
                    Mismatches = JsonSerializer.Serialize(mismatches),
                    Status = "COMPLETED",
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReconciliationResults.Add(result);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order-trade reconciliation completed: {Total} checked, {Mismatches} mismatches",
                    totalChecked, mismatches.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during order-trade reconciliation");

                var result = new ReconciliationResult
                {
                    ReconciliationTime = startTime,
                    EntityType = "ORDERS_TRADES",
                    TotalChecked = totalChecked,
                    MismatchCount = mismatches.Count,
                    Status = "FAILED",
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReconciliationResults.Add(result);
                await _context.SaveChangesAsync();

                return result;
            }
        }

        public async Task<ReconciliationResult> ReconcileWalletBalancesAsync()
        {
            var startTime = DateTime.UtcNow;
            var mismatches = new List<object>();
            int totalChecked = 0;

            try
            {
                _logger.LogInformation("Starting wallet balance reconciliation");

                // Get all wallets
                var wallets = await _context.Wallets
                    .Include(w => w.User)
                    .Include(w => w.Cryptocurrency)
                    .AsNoTracking()
                    .ToListAsync();

                totalChecked = wallets.Count;

                foreach (var wallet in wallets)
                {
                    // Calculate balance from movements
                    var calculatedBalance = await _context.WalletMovements
                        .Where(m => m.WalletId == wallet.Id)
                        .SumAsync(m => m.Amount);

                    // Check for negative balance (should never happen)
                    if (calculatedBalance < 0)
                    {
                        mismatches.Add(new
                        {
                            Type = "NEGATIVE_BALANCE",
                            WalletId = wallet.Id,
                            UserId = wallet.UserId,
                            AssetType = wallet.AssetType,
                            Symbol = wallet.Cryptocurrency?.Symbol ?? wallet.CurrencyCode,
                            Balance = calculatedBalance
                        });
                        _logger.LogError("Wallet {WalletId} for user {UserId} has negative balance: {Balance}",
                            wallet.Id, wallet.UserId, calculatedBalance);
                    }

                    // Check for extremely small balances that might be rounding errors
                    if (calculatedBalance != 0 && Math.Abs(calculatedBalance) < 0.00000001m)
                    {
                        mismatches.Add(new
                        {
                            Type = "ROUNDING_ERROR",
                            WalletId = wallet.Id,
                            UserId = wallet.UserId,
                            Symbol = wallet.Cryptocurrency?.Symbol ?? wallet.CurrencyCode,
                            Balance = calculatedBalance
                        });
                    }

                    // Check available balance vs holds
                    var lockedBalance = await _context.OrderHolds
                        .Where(h => h.WalletId == wallet.Id && h.ReleasedAt == null)
                        .SumAsync(h => h.Amount);

                    var availableBalance = calculatedBalance - lockedBalance;
                    if (availableBalance < 0)
                    {
                        mismatches.Add(new
                        {
                            Type = "OVERLOCKED_BALANCE",
                            WalletId = wallet.Id,
                            UserId = wallet.UserId,
                            Symbol = wallet.Cryptocurrency?.Symbol ?? wallet.CurrencyCode,
                            TotalBalance = calculatedBalance,
                            LockedBalance = lockedBalance,
                            AvailableBalance = availableBalance
                        });
                        _logger.LogError("Wallet {WalletId}: Locked balance exceeds total balance", wallet.Id);
                    }
                }

                var result = new ReconciliationResult
                {
                    ReconciliationTime = startTime,
                    EntityType = "WALLET_BALANCES",
                    TotalChecked = totalChecked,
                    MismatchCount = mismatches.Count,
                    Mismatches = JsonSerializer.Serialize(mismatches),
                    Status = "COMPLETED",
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReconciliationResults.Add(result);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Wallet balance reconciliation completed: {Total} checked, {Mismatches} mismatches",
                    totalChecked, mismatches.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during wallet balance reconciliation");

                var result = new ReconciliationResult
                {
                    ReconciliationTime = startTime,
                    EntityType = "WALLET_BALANCES",
                    TotalChecked = totalChecked,
                    MismatchCount = mismatches.Count,
                    Status = "FAILED",
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReconciliationResults.Add(result);
                await _context.SaveChangesAsync();

                return result;
            }
        }

        public async Task<List<ReconciliationResult>> GetRecentResultsAsync(int count = 10)
        {
            return await _context.ReconciliationResults
                .OrderByDescending(r => r.ReconciliationTime)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }
    }

    /// <summary>
    /// Background service to run reconciliation periodically
    /// </summary>
    public class ReconciliationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReconciliationBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Run every 5 minutes

        public ReconciliationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ReconciliationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reconciliation background service started");

            // Wait 1 minute before first run to let system stabilize
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunReconciliationAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in reconciliation background service");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Reconciliation background service stopped");
        }

        private async Task RunReconciliationAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();

            _logger.LogInformation("Running scheduled reconciliation");

            // Run order-trade reconciliation
            var orderResult = await reconciliationService.ReconcileOrdersAndTradesAsync();
            if (orderResult.MismatchCount > 0)
            {
                _logger.LogWarning("Order-trade reconciliation found {Count} mismatches", orderResult.MismatchCount);
                // TODO: Send alert (email, Slack, etc.)
            }

            // Run wallet balance reconciliation
            var walletResult = await reconciliationService.ReconcileWalletBalancesAsync();
            if (walletResult.MismatchCount > 0)
            {
                _logger.LogWarning("Wallet balance reconciliation found {Count} mismatches", walletResult.MismatchCount);
                // TODO: Send alert (email, Slack, etc.)
            }

            _logger.LogInformation("Scheduled reconciliation completed");
        }
    }
}

