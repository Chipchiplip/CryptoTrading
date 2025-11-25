using CryptoTrading.Data;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Services
{
    public interface ISubscriptionService
    {
        Task<Subscription?> GetUserSubscriptionAsync(int userId);
        Task<Subscription> CreateOrUpdateSubscriptionAsync(int userId, int planType, DateTime periodStart, DateTime periodEnd, string? vnpayTransactionId = null);
        Task<bool> CancelSubscriptionAsync(int userId);
        Task<bool> IsSubscriptionActiveAsync(int userId);
        Task<int> GetUserPlanTypeAsync(int userId);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(ApplicationDbContext context, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Subscription?> GetUserSubscriptionAsync(int userId)
        {
            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<Subscription> CreateOrUpdateSubscriptionAsync(int userId, int planType, DateTime periodStart, DateTime periodEnd, string? vnpayTransactionId = null)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (subscription == null)
            {
                subscription = new Subscription
                {
                    UserId = userId,
                    PlanType = planType,
                    Status = "active",
                    CurrentPeriodStartUtc = periodStart,
                    CurrentPeriodEndUtc = periodEnd,
                    VnpayTransactionId = vnpayTransactionId,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _context.Subscriptions.Add(subscription);
            }
            else
            {
                // Prevent downgrade from active Premium to Free
                bool isDowngrade = planType < subscription.PlanType;
                bool isPremiumActive = subscription.PlanType > 0 && 
                                      subscription.Status == "active" && 
                                      subscription.CurrentPeriodEndUtc > DateTime.UtcNow;
                
                if (isDowngrade && isPremiumActive && planType == 0)
                {
                    throw new InvalidOperationException(
                        "Cannot downgrade to Free plan while Premium subscription is active. " +
                        "Please cancel your subscription and it will automatically downgrade when the current period ends.");
                }

                subscription.PlanType = planType;
                subscription.Status = "active";
                subscription.CurrentPeriodStartUtc = periodStart;
                subscription.CurrentPeriodEndUtc = periodEnd;
                subscription.VnpayTransactionId = vnpayTransactionId;
                subscription.CanceledAtUtc = null;
                subscription.UpdatedAtUtc = DateTime.UtcNow;
            }

            await ApplyUserLevelAsync(userId, planType);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<bool> CancelSubscriptionAsync(int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (subscription == null || subscription.Status != "active")
                return false;

            // Mark as canceled but don't change plan type yet
            // Let it remain active until the period ends
            subscription.Status = "canceled";
            subscription.CanceledAtUtc = DateTime.UtcNow;
            subscription.UpdatedAtUtc = DateTime.UtcNow;

            // Don't downgrade to Free immediately - let background service handle it when period ends
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsSubscriptionActiveAsync(int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "active");

            if (subscription == null)
            {
                await ApplyUserLevelAsync(userId, 0);
                if (_context.ChangeTracker.HasChanges())
                {
                    await _context.SaveChangesAsync();
                }
                return false;
            }

            // Kiểm tra xem subscription có còn hạn không
            if (subscription.CurrentPeriodEndUtc > DateTime.UtcNow)
            {
                return true;
            }

            subscription.Status = "expired";
            subscription.UpdatedAtUtc = DateTime.UtcNow;
            await ApplyUserLevelAsync(userId, 0);
            await _context.SaveChangesAsync();
            return false;
        }

        public async Task<int> GetUserPlanTypeAsync(int userId)
        {
            var subscription = await GetUserSubscriptionAsync(userId);
            
            if (subscription == null)
            {
                return await GetPlanTypeFromUserLevelAsync(userId);
            }

            if (!await IsSubscriptionActiveAsync(userId))
            {
                return await GetPlanTypeFromUserLevelAsync(userId);
            }
            
            return subscription.PlanType;
        }

        private async Task ApplyUserLevelAsync(int userId, int planType)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found while applying subscription level", userId);
                return;
            }

            var levelName = MapPlanTypeToLevel(planType);
            if (!string.Equals(user.Level, levelName, StringComparison.OrdinalIgnoreCase))
            {
                user.Level = levelName;
            }
        }

        private async Task<int> GetPlanTypeFromUserLevelAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return MapLevelToPlanType(user?.Level);
        }

        private static string MapPlanTypeToLevel(int planType) => planType switch
        {
            2 => "Premium",
            _ => "Free"
        };

        private static int MapLevelToPlanType(string? level)
        {
            if (string.IsNullOrWhiteSpace(level))
            {
                return 0;
            }

            return level.Trim().ToLowerInvariant() switch
            {
                "premium" => 2,
                _ => 0
            };
        }
    }
}

