using System.Security.Claims;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Focus.WebAPI.Hubs;

public class RoomHub : Hub<IRoomClient>
{
    private readonly ISender _mediator;

    public RoomHub(ISender mediator)
    {
        _mediator = mediator;
    }

    public async Task JoinRoomGroup(string roomCode)
    {
        if (!string.IsNullOrWhiteSpace(roomCode))
        {
            var groupName = $"room_{roomCode.ToUpperInvariant().Trim()}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
    }

    public async Task LeaveRoomGroup(string roomCode)
    {
        if (!string.IsNullOrWhiteSpace(roomCode))
        {
            var groupName = $"room_{roomCode.ToUpperInvariant().Trim()}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }

    [Authorize]
    public async Task BroadcastReaction(string roomCode, string reaction)
    {
        var subClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId)) return;

        var result = await _mediator.Send(new SendRoomReactionCommand(userId, roomCode, reaction));
        if (result != null)
        {
            var groupName = $"room_{roomCode.ToUpperInvariant().Trim()}";
            await Clients.Group(groupName).ReactionBroadcasted(result);
        }
    }
}
