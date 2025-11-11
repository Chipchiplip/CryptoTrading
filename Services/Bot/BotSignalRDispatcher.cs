using CryptoTrading.Hubs;
using CryptoTrading.Models.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Dispatcher for broadcasting bot events via SignalR
    /// </summary>
    public class BotSignalRDispatcher
    {
        private readonly IHubContext<BotHub> _hubContext;
        private readonly ILogger<BotSignalRDispatcher> _logger;

        public BotSignalRDispatcher(
            IHubContext<BotHub> hubContext,
            ILogger<BotSignalRDispatcher> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Broadcast bot status update
        /// </summary>
        public async Task BroadcastBotStatusAsync(BotStatusUpdatedEvent evt, int userId)
        {
            try
            {
                await _hubContext.Clients
                    .Groups($"User:{userId}", $"Bot:{evt.BotId}")
                    .SendAsync("BotStatusUpdated", evt);

                _logger.LogDebug("Broadcasted status update for bot {BotId}", evt.BotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting bot status");
            }
        }

        /// <summary>
        /// Broadcast bot metrics update
        /// </summary>
        public async Task BroadcastBotMetricsAsync(BotMetricUpdatedEvent evt, int userId)
        {
            try
            {
                await _hubContext.Clients
                    .Groups($"User:{userId}", $"Bot:{evt.BotId}")
                    .SendAsync("BotMetricUpdated", evt);

                _logger.LogDebug("Broadcasted metrics for bot {BotId}", evt.BotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting bot metrics");
            }
        }

        /// <summary>
        /// Broadcast bot execution log
        /// </summary>
        public async Task BroadcastBotLogAsync(BotExecutionLogAppendedEvent evt, int userId)
        {
            try
            {
                await _hubContext.Clients
                    .Groups($"User:{userId}", $"Bot:{evt.BotId}")
                    .SendAsync("BotExecutionLogAppended", evt);

                _logger.LogDebug("Broadcasted log for bot {BotId}", evt.BotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting bot log");
            }
        }

        /// <summary>
        /// Broadcast bot order event
        /// </summary>
        public async Task BroadcastBotOrderAsync(BotOrderEvent evt, int userId)
        {
            try
            {
                await _hubContext.Clients
                    .Groups($"User:{userId}", $"Bot:{evt.BotId}")
                    .SendAsync("BotOrderEvent", evt);

                _logger.LogDebug("Broadcasted order event for bot {BotId}", evt.BotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting bot order");
            }
        }

        /// <summary>
        /// Broadcast bot alert
        /// </summary>
        public async Task BroadcastBotAlertAsync(BotAlertRaisedEvent evt, int userId)
        {
            try
            {
                await _hubContext.Clients
                    .Groups($"User:{userId}", $"Bot:{evt.BotId}")
                    .SendAsync("BotAlertRaised", evt);

                _logger.LogWarning("Broadcasted alert for bot {BotId}: {Message}", evt.BotId, evt.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting bot alert");
            }
        }

        /// <summary>
        /// Broadcast strategy catalog change
        /// </summary>
        public async Task BroadcastStrategyCatalogChangeAsync(StrategyCatalogChangedEvent evt)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"Strategy:{evt.StrategyKey}")
                    .SendAsync("StrategyCatalogChanged", evt);

                _logger.LogInformation("Broadcasted strategy catalog change: {StrategyKey}", evt.StrategyKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting strategy catalog change");
            }
        }
    }
}

