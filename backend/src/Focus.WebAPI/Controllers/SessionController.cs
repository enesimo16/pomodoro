using System.Security.Claims;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.Commands;
using Focus.Application.Features.Session.DTOs;
using Focus.Application.Features.Session.Queries;
using Focus.Domain.Enums;
using Focus.WebAPI.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Focus.WebAPI.Controllers;

public record StartSessionRequest(
    string Kind = "Focus",
    int FocusMinutes = 25,
    int BreakMinutes = 5,
    int CurrentRound = 1,
    int TargetRounds = 4,
    string? ThemeId = null,
    string? MixPresetId = null);

public record ExtendSessionRequest(int ExtraMinutes = 10);

[Authorize]
[Route("api/v1/sessions")]
public class SessionController : BaseApiController
{
    private readonly IHubContext<TimerHub, ITimerClient> _hubContext;

    public SessionController(IHubContext<TimerHub, ITimerClient> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(FocusSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetActiveSession(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var session = await Mediator.Send(new GetActiveSessionQuery(userId), cancellationToken);
        if (session == null)
        {
            return NoContent();
        }

        return Ok(session);
    }

    [HttpPost]
    [ProducesResponseType(typeof(FocusSessionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var kind = Enum.TryParse<SessionKind>(request.Kind, true, out var parsedKind) ? parsedKind : SessionKind.Focus;

        var command = new StartSessionCommand(
            userId,
            kind,
            request.FocusMinutes,
            request.BreakMinutes,
            request.CurrentRound,
            request.TargetRounds,
            request.ThemeId,
            request.MixPresetId);

        var result = await Mediator.Send(command, cancellationToken);

        // SignalR bildirimi
        await _hubContext.Clients.Group($"user_{userId}").TimerStarted(result);
        if (result.Kind == SessionKind.Focus.ToString())
        {
            await _hubContext.Clients.Group($"user_{userId}").DeskLightToggled(true);
        }

        return CreatedAtAction(nameof(GetActiveSession), result);
    }

    [HttpPost("pause")]
    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType(typeof(FocusSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseSession([FromRoute] Guid? id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new PauseSessionCommand(userId, id), cancellationToken);
        if (result == null) return NotFound();

        await _hubContext.Clients.Group($"user_{userId}").TimerPaused(result);
        return Ok(result);
    }

    [HttpPost("resume")]
    [HttpPost("{id:guid}/resume")]
    [ProducesResponseType(typeof(FocusSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeSession([FromRoute] Guid? id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new ResumeSessionCommand(userId, id), cancellationToken);
        if (result == null) return NotFound();

        await _hubContext.Clients.Group($"user_{userId}").TimerResumed(result);
        return Ok(result);
    }

    [HttpPost("extend")]
    [HttpPost("{id:guid}/extend")]
    [ProducesResponseType(typeof(FocusSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExtendSession([FromRoute] Guid? id, [FromBody] ExtendSessionRequest? request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var extraMinutes = request?.ExtraMinutes ?? 10;
        var result = await Mediator.Send(new ExtendSessionCommand(userId, extraMinutes, id), cancellationToken);
        if (result == null) return NotFound();

        await _hubContext.Clients.Group($"user_{userId}").TimerExtended(result);
        return Ok(result);
    }

    [HttpPost("complete")]
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(SessionCompletionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteSession([FromRoute] Guid? id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new CompleteSessionCommand(userId, id), cancellationToken);
        if (result == null) return NotFound();

        await _hubContext.Clients.Group($"user_{userId}").TimerCompleted(result);
        await _hubContext.Clients.Group($"user_{userId}").DeskLightToggled(false);

        return Ok(result);
    }

    [HttpPost("abandon")]
    [HttpPost("{id:guid}/abandon")]
    [ProducesResponseType(typeof(FocusSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AbandonSession([FromRoute] Guid? id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new AbandonSessionCommand(userId, id), cancellationToken);
        if (result == null) return NotFound();

        await _hubContext.Clients.Group($"user_{userId}").TimerAbandoned(result);
        await _hubContext.Clients.Group($"user_{userId}").DeskLightToggled(false);

        return Ok(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(subClaim, out userId);
    }
}
