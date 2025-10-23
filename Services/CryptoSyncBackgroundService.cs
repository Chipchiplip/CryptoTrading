namespace CryptoTrading.Services
{
    public class CryptoSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CryptoSyncBackgroundService> _logger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5); // Sync every 5 minutes

        public CryptoSyncBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<CryptoSyncBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Crypto Sync Background Service is starting");

            // Wait 10 seconds after app starts before first sync
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformSyncAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during crypto sync");
                }

                _logger.LogInformation("Next sync in {Minutes} minutes", _syncInterval.TotalMinutes);
                await Task.Delay(_syncInterval, stoppingToken);
            }

            _logger.LogInformation("Crypto Sync Background Service is stopping");
        }

        private async Task PerformSyncAsync()
        {
            _logger.LogInformation("Starting scheduled crypto sync...");

            using (var scope = _serviceProvider.CreateScope())
            {
                var syncService = scope.ServiceProvider.GetRequiredService<ICryptoDataSyncService>();

                try
                {
                    // Sync cryptocurrencies first (to ensure they exist in DB)
                    await syncService.SyncCryptocurrenciesAsync();

                    // Then sync prices
                    await syncService.SyncPricesAsync();

                    // Finally sync market stats
                    await syncService.SyncMarketStatsAsync();

                    _logger.LogInformation("Scheduled crypto sync completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in scheduled crypto sync");
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Crypto Sync Background Service is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}

