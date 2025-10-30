using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoTrading.Services
{
    /// <summary>
    /// Lightweight broadcaster to push market snapshots to SignalR every 5 seconds.
    /// Uses CoinGeckoService which will respect cache expiry and external rate limits.
    /// </summary>
    public class RealtimeBroadcastService : BackgroundService
    {
        private readonly ILogger<RealtimeBroadcastService> _logger;
        private readonly ICoinGeckoService _coinGecko;

        public RealtimeBroadcastService(ILogger<RealtimeBroadcastService> logger, ICoinGeckoService coinGecko)
        {
            _logger = logger;
            _coinGecko = coinGecko;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Trigger fetch + broadcast (CoinGeckoService broadcasts after fetch)
                    await _coinGecko.GetMarketDataAsync();
                    await _coinGecko.GetMarketStatsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Realtime broadcast error");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch { }
            }
        }
    }
}


