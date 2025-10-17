using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace CryptoTrading.SignalR;

[Authorize]
public class TradingHub : Hub
{
    public async Task JoinUserGroup()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            await Clients.Caller.SendAsync("JoinedUserGroup", userId);
        }
    }

    public async Task LeaveUserGroup()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
            await Clients.Caller.SendAsync("LeftUserGroup", userId);
        }
    }

    public override async Task OnConnectedAsync()
    {
        // Automatically join user's personal group
        await JoinUserGroup();
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
