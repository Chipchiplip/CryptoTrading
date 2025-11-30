using CryptoTrading.Services.Trading;
using CryptoTrading.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services
{
    /// <summary>
    /// Background service that periodically matches pending limit orders
    /// Runs every 10 seconds to find matching buy/sell orders (optimized with pending check)
    /// </summary>
    public class OrderMatchingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderMatchingBackgroundService> _logger;
        private const int MATCHING_INTERVAL_SECONDS = 10; // ✅ Tăng từ 5 → 10 giây (vì đã có immediate matching)
        private const int CLEANUP_INTERVAL_ITERATIONS = 6; // Cleanup mỗi 6 lần = ~1 phút
        private int _iterationCount = 0;

        public OrderMatchingBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<OrderMatchingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Executes the background service
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderMatchingBackgroundService is starting");

            // Wait a few seconds before starting to allow services to initialize
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ✅ Check if there are pending orders first (avoid unnecessary work)
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    var hasPendingOrders = await dbContext.Orders
                        .AnyAsync(o => o.Type == "LIMIT" && 
                                      (o.Status == "NEW" || o.Status == "PARTIAL"),
                                      stoppingToken);
                    
                    if (hasPendingOrders)
                    {
                        var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
                        var matchCount = await tradingService.MatchOrdersAsync();
                        
                        if (matchCount > 0)
                        {
                            _logger.LogInformation("Background matching completed: {Count} matches", matchCount);
                        }
                    }

                    // Also update order statuses for orders that are fully filled but status wasn't updated
                    var tradingServiceForStatus = scope.ServiceProvider.GetRequiredService<ITradingService>();
                    if (tradingServiceForStatus is TradingService tradingServiceImpl)
                    {
                        var updatedCount = await tradingServiceImpl.UpdateFilledOrderStatusesAsync();
                        if (updatedCount > 0)
                        {
                            _logger.LogInformation("Updated {Count} filled order statuses", updatedCount);
                        }
                        
                        // Cleanup orphaned OrderHolds periodically (every 6 iterations = ~1 minute)
                        // This prevents cash from being locked indefinitely
                        _iterationCount++;
                        if (_iterationCount >= CLEANUP_INTERVAL_ITERATIONS)
                        {
                            _iterationCount = 0;
                            var cleanupCount = await tradingServiceImpl.CleanupOrphanedOrderHoldsAsync();
                            if (cleanupCount > 0)
                            {
                                _logger.LogInformation("Cleaned up {Count} orphaned OrderHolds", cleanupCount);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during order matching");
                }

                // Wait for the next interval
                await Task.Delay(TimeSpan.FromSeconds(MATCHING_INTERVAL_SECONDS), stoppingToken);
            }

            _logger.LogInformation("OrderMatchingBackgroundService is stopping");
        }

        // Note: MatchOrdersAsync() method removed - logic now in ExecuteAsync() for better optimization

        /// <summary>
        /// Called when the service is stopping
        /// </summary>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OrderMatchingBackgroundService is being stopped");
            return base.StopAsync(cancellationToken);
        }
    }
}

