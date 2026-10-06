using Focus.Application.Features.Stats.DTOs;
using Focus.Application.Features.Stats.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[Authorize]
public class StatsController : BaseApiController
{
    /// <summary>
    /// Kullanıcının bugün, bu hafta, bu ay tamamladığı odak seansı sayılarını, dakikalarını ve son 7 günlük çubuk grafik dağılımını getirir.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new GetUserFocusStatsQuery(userId), cancellationToken);
        return Ok(result);
    }
}
