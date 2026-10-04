using Focus.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Focus.WebAPI.Hubs;

[Authorize]
public class TimerHub : Hub<ITimerClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public async Task SubscribeToUser(string targetUserId)
    {
        if (!string.IsNullOrWhiteSpace(targetUserId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{targetUserId}");
        }
    }

    public async Task UnsubscribeFromUser(string targetUserId)
    {
        if (!string.IsNullOrWhiteSpace(targetUserId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{targetUserId}");
        }
    }
}
