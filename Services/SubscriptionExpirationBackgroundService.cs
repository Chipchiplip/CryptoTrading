using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services
{
    /// <summary>
    /// Background service that automatically renews expired subscriptions or downgrades to Free plan
    /// Runs every hour to check for expired subscriptions
    /// </summary>
    public class SubscriptionExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriptionExpirationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

        public SubscriptionExpirationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<SubscriptionExpirationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Subscription Expiration Background Service is starting");

            // Wait 30 seconds after app starts before first check
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndRenewExpiredSubscriptionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during subscription expiration check");
                }

                _logger.LogDebug("Next subscription expiration check in {Hours} hours", _checkInterval.TotalHours);
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Subscription Expiration Background Service is stopping");
        }

        private async Task CheckAndRenewExpiredSubscriptionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

            try
            {
                var now = DateTime.UtcNow;

                // Find all active subscriptions that have expired (only paid plans)
                var expiredSubscriptions = await context.Subscriptions
                    .Where(s => 
                        s.Status == "active" && 
                        s.CurrentPeriodEndUtc <= now &&
                        s.PlanType > 0) // Only paid plans (not Free)
                    .ToListAsync();

                if (expiredSubscriptions.Count == 0)
                {
                    _logger.LogDebug("No expired subscriptions found");
                    return;
                }

                _logger.LogInformation("Found {Count} expired subscriptions to process", expiredSubscriptions.Count);

                // Plan prices in VND
                var planPricesVnd = new Dictionary<int, decimal>
                {
                    { 2, 2376000m } // Premium: ~99 USD
                };

                var planNames = new Dictionary<int, string>
                {
                    { 0, "Free" },
                    { 2, "Premium" }
                };

                const decimal exchangeRate = 24000m; // VND to USD

                foreach (var subscription in expiredSubscriptions)
                {
                    try
                    {
                        var planType = subscription.PlanType;
                        var amountVnd = planPricesVnd[planType];
                        var amountUsd = amountVnd / exchangeRate;

                        // Get or create USD wallet
                        var usdWallet = await GetOrCreateWalletAsync(context, subscription.UserId, "FIAT", "USD", null);
                        var availableBalance = await CalculateAvailableBalanceAsync(context, usdWallet.Id);

                        if (availableBalance >= amountUsd)
                        {
                            // Sufficient balance - renew subscription
                            using var transaction = await context.Database.BeginTransactionAsync();
                            try
                            {
                                // Deduct from wallet
                                var walletMovement = new WalletMovement
                                {
                                    WalletId = usdWallet.Id,
                                    RefType = "SUBSCRIPTION_RENEWAL",
                                    RefId = null,
                                    Amount = -amountUsd,
                                    Note = $"Auto-renewal: {planNames[planType]} plan - {amountVnd:N0} VND ({amountUsd:N2} USD)",
                                    CreatedAt = DateTime.UtcNow
                                };

                                context.WalletMovements.Add(walletMovement);

                                // Create payment history
                                var orderId = $"RENEW_{subscription.UserId}_{DateTime.UtcNow.Ticks}";
                                var paymentHistory = new PaymentHistory
                                {
                                    UserId = subscription.UserId,
                                    Amount = amountVnd,
                                    Currency = "VND",
                                    Status = "success",
                                    VnpayOrderId = orderId,
                                    PaymentMethod = "WALLET",
                                    PlanType = planType,
                                    CreatedAtUtc = DateTime.UtcNow,
                                    UpdatedAtUtc = DateTime.UtcNow
                                };

                                context.PaymentHistories.Add(paymentHistory);
                                await context.SaveChangesAsync();

                                // Renew subscription
                                var periodStart = now;
                                var periodEnd = periodStart.AddMonths(1);

                                subscription.PlanType = planType;
                                subscription.Status = "active";
                                subscription.CurrentPeriodStartUtc = periodStart;
                                subscription.CurrentPeriodEndUtc = periodEnd;
                                subscription.VnpayTransactionId = orderId;
                                subscription.CanceledAtUtc = null;
                                subscription.UpdatedAtUtc = now;

                                await subscriptionService.CreateOrUpdateSubscriptionAsync(
                                    subscription.UserId,
                                    planType,
                                    periodStart,
                                    periodEnd,
                                    orderId
                                );

                                paymentHistory.SubscriptionId = subscription.Id;
                                await context.SaveChangesAsync();

                                // Create success notification
                                var notification = new Notification
                                {
                                    UserId = subscription.UserId,
                                    Type = "success",
                                    Title = "Subscription Renewed Successfully",
                                    Message = $"Your {planNames[planType]} subscription has been automatically renewed. ${amountUsd:N2} USD has been deducted from your wallet. Next renewal: {periodEnd:MMMM dd, yyyy}",
                                    Category = "subscription",
                                    IsRead = false,
                                    CreatedAtUtc = DateTime.UtcNow
                                };

                                context.Notifications.Add(notification);
                                await context.SaveChangesAsync();
                                await transaction.CommitAsync();

                                _logger.LogInformation(
                                    "Auto-renewed subscription successfully: UserId={UserId}, PlanType={PlanType}, AmountUSD={AmountUSD}",
                                    subscription.UserId, planType, amountUsd);
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                _logger.LogError(ex, "Error renewing subscription for UserId={UserId}", subscription.UserId);
                                throw;
                            }
                        }
                        else
                        {
                            // Insufficient balance - downgrade to Free plan
                            var periodStart = now;
                            var periodEnd = periodStart.AddYears(100); // Free plan doesn't expire

                            await subscriptionService.CreateOrUpdateSubscriptionAsync(
                                subscription.UserId,
                                0, // Free plan
                                periodStart,
                                periodEnd
                            );

                            subscription.Status = "expired";
                            subscription.UpdatedAtUtc = now;
                            await context.SaveChangesAsync();

                            // Create failure notification
                            var notification = new Notification
                            {
                                UserId = subscription.UserId,
                                Type = "warning",
                                Title = "Subscription Expired - Insufficient Balance",
                                Message = $"Your {planNames[planType]} subscription has expired. We couldn't auto-renew because your wallet balance (${availableBalance:N2} USD) is insufficient. Required: ${amountUsd:N2} USD. Your subscription has been downgraded to Free plan.",
                                Category = "subscription",
                                IsRead = false,
                                CreatedAtUtc = DateTime.UtcNow
                            };

                            context.Notifications.Add(notification);
                            await context.SaveChangesAsync();

                            _logger.LogInformation(
                                "Downgraded expired subscription to Free plan due to insufficient balance: UserId={UserId}, PreviousPlan={PreviousPlan}, Required={Required}, Available={Available}",
                                subscription.UserId, planType, amountUsd, availableBalance);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, 
                            "Error processing subscription renewal for UserId={UserId}", 
                            subscription.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAndRenewExpiredSubscriptionsAsync");
                throw;
            }
        }

        private async Task<Wallet> GetOrCreateWalletAsync(ApplicationDbContext context, int userId, string assetType, string? currencyCode, int? cryptoId)
        {
            var wallet = await context.Wallets
                .FirstOrDefaultAsync(w =>
                    w.UserId == userId &&
                    w.AssetType == assetType &&
                    w.CurrencyCode == currencyCode &&
                    w.CryptocurrencyId == cryptoId);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    AssetType = assetType,
                    CurrencyCode = currencyCode,
                    CryptocurrencyId = cryptoId
                };
                context.Wallets.Add(wallet);
                await context.SaveChangesAsync();
            }

            return wallet;
        }

        private async Task<decimal> CalculateAvailableBalanceAsync(ApplicationDbContext context, ulong walletId)
        {
            // Get total balance from wallet movements
            var totalBalance = await context.WalletMovements
                .Where(m => m.WalletId == walletId)
                .SumAsync(m => (decimal?)m.Amount) ?? 0m;

            // Get locked balance from active order holds
            var lockedBalance = await context.OrderHolds
                .Where(h => h.WalletId == walletId && h.ReleasedAt == null)
                .SumAsync(h => (decimal?)h.Amount) ?? 0m;

            return totalBalance - lockedBalance;
        }
    }
}

