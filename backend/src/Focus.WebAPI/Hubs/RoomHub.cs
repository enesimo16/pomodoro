using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.Commands;
using Focus.Application.Features.Social.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Focus.WebAPI.Hubs;

[Authorize]
public class RoomHub : Hub<IRoomClient>
{
    private readonly ISender _mediator;

    public RoomHub(ISender mediator)
    {
        _mediator = mediator;
    }

    public async Task JoinRoomGroup(string roomCode)
    {
        var userId = RequireUserId();
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            throw new HubException("Oda bulunamadı.");
        }

        var isMember = await _mediator.Send(new IsRoomMemberQuery(userId, roomCode), Context.ConnectionAborted);
        if (!isMember)
        {
            throw new HubException("Bu odaya erişim yetkiniz yok.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(roomCode), Context.ConnectionAborted);
    }

    public async Task Heartbeat(string roomCode)
    {
        var userId = RequireUserId();
        if (string.IsNullOrWhiteSpace(roomCode)) return;

        var isMember = await _mediator.Send(new TouchRoomMemberCommand(userId, roomCode), Context.ConnectionAborted);
        if (!isMember)
        {
            throw new HubException("Bu odaya erişim yetkiniz yok.");
        }
    }

    public async Task LeaveRoomGroup(string roomCode)
    {
        if (!string.IsNullOrWhiteSpace(roomCode))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(roomCode), Context.ConnectionAborted);
        }
    }

    public async Task BroadcastReaction(string roomCode, string reaction)
    {
        var userId = RequireUserId();

        var result = await _mediator.Send(new SendRoomReactionCommand(userId, roomCode, reaction), Context.ConnectionAborted);
        if (result != null)
        {
            await Clients.Group(GroupName(roomCode)).ReactionBroadcasted(result);
        }
    }

    public static string GroupName(string roomCode) => $"room_{roomCode.ToUpperInvariant().Trim()}";

    private Guid RequireUserId()
    {
        return Guid.TryParse(Context.UserIdentifier, out var userId)
            ? userId
            : throw new HubException("Oturum doğrulanamadı.");
    }
}
