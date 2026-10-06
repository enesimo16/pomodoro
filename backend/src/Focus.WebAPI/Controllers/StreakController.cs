using Focus.Application.Features.Streak.DTOs;
using Focus.Application.Features.Streak.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[Authorize]
public class StreakController : BaseApiController
{
    /// <summary>
    /// Kullanıcının güncel seri gün sayısını (Streak), en uzun serisini, kalan dondurucu haklarını ve serinin risk durumunu getirir.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserStreakDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStreak(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new GetUserStreakQuery(userId), cancellationToken);
        return Ok(result);
    }
}
