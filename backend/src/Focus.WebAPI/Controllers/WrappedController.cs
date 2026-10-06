using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Wrapped.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[Authorize]
[Route("api/v1/wrapped")]
public class WrappedController : BaseApiController
{
    private readonly IFocusWrappedService _wrappedService;

    public WrappedController(IFocusWrappedService wrappedService)
    {
        _wrappedService = wrappedService;
    }

    /// <summary>
    /// Kullanıcının son 7 günlük odaklanma seanslarından derlenen haftalık Focus Wrapped özetini getirir.
    /// </summary>
    [HttpGet("weekly")]
    [ProducesResponseType(typeof(FocusWrappedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWeeklyWrapped(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _wrappedService.GetWeeklyWrappedAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının son 30 günlük odaklanma performansını, kronotipini ve müzik/tema tercihlerini özetleyen aylık Focus Wrapped karnesini getirir.
    /// </summary>
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(FocusWrappedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthlyWrapped(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _wrappedService.GetMonthlyWrappedAsync(userId, cancellationToken);
        return Ok(result);
    }
}
