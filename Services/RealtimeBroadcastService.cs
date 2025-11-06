using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoTrading.Services
{
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
            // ✅ Tăng interval lên 5 giây để giảm API calls (vì cache đã 30s)
            var baseInterval = TimeSpan.FromSeconds(5); // Was 2 seconds
            var backoff = TimeSpan.Zero;
            var maxBackoff = TimeSpan.FromSeconds(60);
            var rand = new Random();
            var toggle = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ✅ Force refresh để có data mới nhất cho real-time updates
                    // So le giữa market data và stats để giảm xác suất va chạm hạn mức
                    if (toggle)
                    {
                        await _coinGecko.GetMarketDataAsync(forceRefresh: true); // ✅ Force refresh để bypass cache
                    }
                    else
                    {
                        await _coinGecko.GetMarketStatsAsync();
                    }
                    toggle = !toggle;

                    // Giảm backoff dần khi đã thành công
                    if (backoff > TimeSpan.Zero)
                    {
                        backoff = TimeSpan.FromMilliseconds(Math.Max(0, backoff.TotalMilliseconds / 2));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Realtime broadcast error, applying backoff");
                    backoff = backoff == TimeSpan.Zero
                        ? TimeSpan.FromSeconds(5)
                        : TimeSpan.FromSeconds(Math.Min(maxBackoff.TotalSeconds, backoff.TotalSeconds * 2));
                }

                var jitter = TimeSpan.FromMilliseconds(rand.Next(100, 300));
                try
                {
                    await Task.Delay(baseInterval + backoff + jitter, stoppingToken);
                }
                catch { }
            }
        }
    }
}


