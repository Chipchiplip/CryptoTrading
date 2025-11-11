using CryptoTrading.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Monitors bot health, heartbeats, and cleanup
    /// </summary>
    public class BotMonitorHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BotMonitorHostedService> _logger;
        private const int HEARTBEAT_INTERVAL_SECONDS = 5;
        private const int DEGRADED_THRESHOLD_SECONDS = 300; // 5 minutes

        public BotMonitorHostedService(
            IServiceProvider serviceProvider,
            ILogger<BotMonitorHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bot Monitor Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorBotsAsync(stoppingToken);
                    await CleanupOldSnapshotsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in monitor service");
                }

                await Task.Delay(TimeSpan.FromSeconds(HEARTBEAT_INTERVAL_SECONDS), stoppingToken);
            }

            _logger.LogInformation("Bot Monitor Service stopped");
        }

        private async Task MonitorBotsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var runningBots = await context.TradingBots
                .Where(b => b.Status == "Running")
                .ToListAsync(stoppingToken);

            foreach (var bot in runningBots)
            {
                // Check if bot has been idle too long
                if (bot.UpdatedAt.HasValue)
                {
                    var timeSinceLastUpdate = DateTime.UtcNow - bot.UpdatedAt.Value;
                    if (timeSinceLastUpdate.TotalSeconds > DEGRADED_THRESHOLD_SECONDS)
                    {
                        bot.Status = "Degraded";
                        bot.LastStatusReason = "Bot appears to be stuck or not executing";
                        _logger.LogWarning("Bot {BotId} marked as degraded (idle for {Seconds}s)", 
                            bot.Id, timeSinceLastUpdate.TotalSeconds);
                    }
                }
            }

            // Handle stopping bots
            var stoppingBots = await context.TradingBots
                .Where(b => b.Status == "Stopping")
                .ToListAsync(stoppingToken);

            foreach (var stoppingBot in stoppingBots)
            {
                // Cancel any pending orders (TODO)
                stoppingBot.Status = "Stopped";
                stoppingBot.NextRunAt = null;
                stoppingBot.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Bot {BotId} stopped", stoppingBot.Id);
            }

            if (runningBots.Any() || stoppingBots.Any())
            {
                await context.SaveChangesAsync(stoppingToken);
            }
        }

        private async Task CleanupOldSnapshotsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Keep only last 100 snapshots per bot
            var botIds = await context.TradingBots.Select(b => b.Id).ToListAsync(stoppingToken);

            foreach (var botId in botIds)
            {
                var snapshotsToDelete = await context.TradingBotRuntimeSnapshots
                    .Where(s => s.TradingBotId == botId)
                    .OrderByDescending(s => s.CapturedAt)
                    .Skip(100)
                    .ToListAsync(stoppingToken);

                if (snapshotsToDelete.Any())
                {
                    context.TradingBotRuntimeSnapshots.RemoveRange(snapshotsToDelete);
                    _logger.LogDebug("Cleaned up {Count} old snapshots for bot {BotId}", 
                        snapshotsToDelete.Count, botId);
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}

