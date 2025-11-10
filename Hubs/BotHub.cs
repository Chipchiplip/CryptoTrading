using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CryptoTrading.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time bot updates
    /// </summary>
    [Authorize]
    public class BotHub : Hub
    {
        private readonly ILogger<BotHub> _logger;

        public BotHub(ILogger<BotHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Add to user-specific group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User:{userId}");
                _logger.LogInformation("User {UserId} connected to BotHub", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User:{userId}");
                _logger.LogInformation("User {UserId} disconnected from BotHub", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribe to specific bot updates
        /// </summary>
        public async Task SubscribeToBot(string botId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Bot:{botId}");
            _logger.LogDebug("Connection {ConnectionId} subscribed to bot {BotId}", 
                Context.ConnectionId, botId);
        }

        /// <summary>
        /// Unsubscribe from bot updates
        /// </summary>
        public async Task UnsubscribeFromBot(string botId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Bot:{botId}");
            _logger.LogDebug("Connection {ConnectionId} unsubscribed from bot {BotId}", 
                Context.ConnectionId, botId);
        }

        /// <summary>
        /// Subscribe to strategy updates
        /// </summary>
        public async Task SubscribeToStrategy(string strategyKey)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Strategy:{strategyKey}");
            _logger.LogDebug("Connection {ConnectionId} subscribed to strategy {StrategyKey}", 
                Context.ConnectionId, strategyKey);
        }

        /// <summary>
        /// Unsubscribe from strategy updates
        /// </summary>
        public async Task UnsubscribeFromStrategy(string strategyKey)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Strategy:{strategyKey}");
            _logger.LogDebug("Connection {ConnectionId} unsubscribed from strategy {StrategyKey}", 
                Context.ConnectionId, strategyKey);
        }
    }
}

