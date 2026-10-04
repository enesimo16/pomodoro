using System.Security.Claims;
using Focus.Application.Features.Stats.DTOs;
using Focus.Application.Features.Stats.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[Authorize]
public class StatsController : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(UserStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await Mediator.Send(new GetUserFocusStatsQuery(userId), cancellationToken);
        return Ok(result);
    }
}
