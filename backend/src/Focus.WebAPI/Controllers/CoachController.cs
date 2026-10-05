using System.Security.Claims;
using Focus.Application.Features.Coach.Commands;
using Focus.Application.Features.Coach.DTOs;
using Focus.Application.Features.Coach.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[Route("api/v1/coach")]
public class CoachController : BaseApiController
{
    [Authorize]
    [HttpPost("chat")]
    [ProducesResponseType(typeof(CoachChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Chat([FromBody] CoachChatRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Mesaj alani bos olamaz." });
        }

        var result = await Mediator.Send(new SendCoachChatCommand(userId, request.Message), cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(SessionCheckInDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckIn([FromBody] SessionCheckInRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var result = await Mediator.Send(new SubmitSessionCheckInCommand(
                userId,
                request.SessionId,
                request.Mood,
                request.EnergyLevel,
                request.TargetIntent), cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("reflection")]
    [ProducesResponseType(typeof(SessionReflectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Reflection([FromBody] SessionReflectionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var result = await Mediator.Send(new SubmitSessionReflectionCommand(
                userId,
                request.SessionId,
                request.FocusQuality,
                request.MoodAfter,
                request.DistractionNote), cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("anomaly-status")]
    [ProducesResponseType(typeof(CoachAnomalyStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAnomalyStatus(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new GetCoachAnomalyStatusQuery(userId), cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("memories")]
    [ProducesResponseType(typeof(List<MemoryInsightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMemories(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new GetRecentMemoriesQuery(userId), cancellationToken);
        return Ok(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(subClaim, out userId);
    }
}
