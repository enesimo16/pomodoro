using System.Security.Claims;
using Focus.Application.Features.Streak.DTOs;
using Focus.Application.Features.Streak.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[Authorize]
public class StreakController : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(UserStreakDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStreak(CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await Mediator.Send(new GetUserStreakQuery(userId), cancellationToken);
        return Ok(result);
    }
}
