using Microsoft.AspNetCore.SignalR;
using CryptoTrading.Models;

namespace CryptoTrading.Hubs
{
    public class MarketHub : Hub
    {
        public async Task JoinMarketGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "MarketUpdates");
        }

        public async Task LeaveMarketGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "MarketUpdates");
        }

        public async Task SendPriceUpdate(Crypto coin)
        {
            await Clients.All.SendAsync("ReceivePriceUpdate", coin);
        }

        public async Task SendMarketStats(MarketStats stats)
        {
            await Clients.All.SendAsync("ReceiveMarketStats", stats);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
