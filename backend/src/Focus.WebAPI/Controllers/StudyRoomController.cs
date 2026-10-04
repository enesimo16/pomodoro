using System.Security.Claims;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.Commands;
using Focus.Application.Features.Social.DTOs;
using Focus.Application.Features.Social.Queries;
using Focus.WebAPI.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Focus.WebAPI.Controllers;

public record JoinRoomRequest(string RoomCode, string? InviteCode = null);
public record InviteUsernameRequest(string TargetUsername);
public record SitAtSeatRequest(string RoomCode, int SeatIndex);
public record RoomReactionRequest(string RoomCode, string Reaction);

[Route("api/v1/study-rooms")]
public class StudyRoomController : BaseApiController
{
    private readonly IHubContext<RoomHub, IRoomClient> _roomHub;

    public StudyRoomController(IHubContext<RoomHub, IRoomClient> roomHub)
    {
        _roomHub = roomHub;
    }

    [HttpGet("public")]
    [ProducesResponseType(typeof(List<StudyRoomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicRooms(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPublicRoomsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{code}")]
    [ProducesResponseType(typeof(StudyRoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomByCode([FromRoute] string code, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetRoomByCodeQuery(code), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize]
    [HttpPost("create-invite")]
    [ProducesResponseType(typeof(RoomInvitationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateInvite(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await Mediator.Send(new CreateRoomInvitationCommand(userId, baseUrl), cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("invite-username")]
    [ProducesResponseType(typeof(RoomInvitationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InviteUsername([FromBody] InviteUsernameRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await Mediator.Send(new InviteUserByUsernameCommand(userId, request.TargetUsername, baseUrl), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("join")]
    [ProducesResponseType(typeof(JoinRoomResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JoinRoomResultDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> JoinRoom([FromBody] JoinRoomRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new JoinRoomCommand(userId, request.RoomCode, request.InviteCode), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        // SignalR odaya katilma yayini
        var joinedMember = result.Room?.Members.FirstOrDefault(m => m.UserId == userId);
        if (joinedMember != null)
        {
            var groupName = $"room_{request.RoomCode.ToUpperInvariant().Trim()}";
            await _roomHub.Clients.Group(groupName).UserJoinedRoom(joinedMember);
            if (result.Room != null)
            {
                await _roomHub.Clients.Group(groupName).CoWorkingBonusUpdated(result.Room.CoWorkingBonusPercent);
            }
        }

        return Ok(result);
    }

    [Authorize]
    [HttpPost("leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LeaveRoom([FromBody] JoinRoomRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var success = await Mediator.Send(new LeaveRoomCommand(userId, request.RoomCode), cancellationToken);
        if (success)
        {
            var groupName = $"room_{request.RoomCode.ToUpperInvariant().Trim()}";
            await _roomHub.Clients.Group(groupName).UserLeftRoom(userId);
        }

        return Ok(new { success });
    }

    [Authorize]
    [HttpPost("sit")]
    [ProducesResponseType(typeof(SitSeatResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SitSeatResultDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SitAtSeat([FromBody] SitAtSeatRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new SitAtSeatCommand(userId, request.RoomCode, request.SeatIndex), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        var groupName = $"room_{request.RoomCode.ToUpperInvariant().Trim()}";
        await _roomHub.Clients.Group(groupName).SeatOccupied(userId, request.SeatIndex);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("reaction")]
    [ProducesResponseType(typeof(RoomReactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendReaction([FromBody] RoomReactionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new SendRoomReactionCommand(userId, request.RoomCode, request.Reaction), cancellationToken);
        if (result == null)
        {
            return BadRequest(new { message = "Reaksiyon gönderilemedi. Önce odaya katılmalısınız." });
        }

        var groupName = $"room_{request.RoomCode.ToUpperInvariant().Trim()}";
        await _roomHub.Clients.Group(groupName).ReactionBroadcasted(result);

        return Ok(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(subClaim, out userId);
    }
}
