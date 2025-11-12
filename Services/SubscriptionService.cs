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
                subscription.PlanType = planType;
                subscription.Status = "active";
                subscription.CurrentPeriodStartUtc = periodStart;
                subscription.CurrentPeriodEndUtc = periodEnd;
                subscription.VnpayTransactionId = vnpayTransactionId;
                subscription.CanceledAtUtc = null;
                subscription.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<bool> CancelSubscriptionAsync(int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (subscription == null || subscription.Status != "active")
                return false;

            subscription.Status = "canceled";
            subscription.CanceledAtUtc = DateTime.UtcNow;
            subscription.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsSubscriptionActiveAsync(int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "active");

            if (subscription == null)
                return false;

            // Kiểm tra xem subscription có còn hạn không
            return subscription.CurrentPeriodEndUtc > DateTime.UtcNow;
        }

        public async Task<int> GetUserPlanTypeAsync(int userId)
        {
            var subscription = await GetUserSubscriptionAsync(userId);
            
            if (subscription == null || !await IsSubscriptionActiveAsync(userId))
                return 0; // Free plan
            
            return subscription.PlanType;
        }
    }
}

