using CryptoTrading.Services.Trading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services
{
    /// <summary>
    /// Background service that periodically matches pending limit orders
    /// Runs every 5 seconds to find matching buy/sell orders
    /// </summary>
    public class OrderMatchingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderMatchingBackgroundService> _logger;
        private const int MATCHING_INTERVAL_SECONDS = 5;

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
                    await MatchOrdersAsync();
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

        /// <summary>
        /// Performs order matching using a scoped service instance
        /// </summary>
        private async Task MatchOrdersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();

            try
            {
                var matchCount = await tradingService.MatchOrdersAsync();

                if (matchCount > 0)
                {
                    _logger.LogInformation("Order matching completed: {Count} matches made", matchCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MatchOrdersAsync");
            }
        }

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

