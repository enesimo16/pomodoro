using Focus.Application.Features.Atmosphere.Commands;
using Focus.Application.Features.Atmosphere.DTOs;
using Focus.Application.Features.Atmosphere.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[Route("api/v1/ai-atmosphere")]
public class AtmosphereAiController : BaseApiController
{
    /// <summary>
    /// Kullanıcının serbest metin prompt'u veya seçtiği tercihlere göre tam bir piksel oda teması ve 4 kanallı ses mikseri üretir.
    /// </summary>
    [Authorize]
    [HttpPost("generate")]
    [ProducesResponseType(typeof(GeneratedAtmosphereDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Generate([FromBody] GenerateAtmosphereRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new GenerateAtmosphereCommand(userId, request), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının anlık yorgunluk skoru, sirkadiyen dilimi, yerel hava durumu ve kolektif yapay zeka hafızasına göre otomatik tema ve müzik önerir.
    /// </summary>
    [Authorize]
    [HttpGet("auto-recommend")]
    [ProducesResponseType(typeof(GeneratedAtmosphereDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AutoRecommend(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await Mediator.Send(new GetAutoAtmosphereQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Üretilen atmosferin oda duvar/zemin kaplamasını odaya ve seans süresi/flow shield ayarlarını kullanıcı tercihlerine uygular.
    /// </summary>
    [Authorize]
    [HttpPost("apply")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Apply([FromBody] ApplyAtmosphereRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var success = await Mediator.Send(new ApplyAtmosphereCommand(userId, request), cancellationToken);
        return Ok(new { success });
    }

    /// <summary>
    /// Üretilen atmosferi kullanıcının kişisel kütüphanesine kaydeder.
    /// </summary>
    [Authorize]
    [HttpPost("save")]
    [ProducesResponseType(typeof(SavedAtmosphereDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Save([FromBody] SaveAtmosphereApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (request.Atmosphere == null)
        {
            return BadRequest(new { message = "Kaydedilecek atmosfer nesnesi gereklidir." });
        }

        var customName = !string.IsNullOrWhiteSpace(request.CustomName) ? request.CustomName : request.Name;
        var result = await Mediator.Send(new SaveAtmosphereCommand(userId, request.Atmosphere, customName), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının kaydettiği tüm özel atmosferleri listeler.
    /// </summary>
    [Authorize]
    [HttpGet("saved")]
    [ProducesResponseType(typeof(List<SavedAtmosphereDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSaved(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var list = await Mediator.Send(new GetSavedAtmospheresQuery(userId), cancellationToken);
        return Ok(list);
    }

    /// <summary>
    /// Kaydedilmiş bir özel atmosferi siler.
    /// </summary>
    [Authorize]
    [HttpDelete("saved/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteSaved(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var success = await Mediator.Send(new DeleteSavedAtmosphereCommand(userId, id), cancellationToken);
        if (!success) return NotFound(new { message = "Atmosfer bulunamadi veya silinemedi." });

        return Ok(new { success });
    }

    /// <summary>
    /// Başarıyla tamamlanan bir seansın tema ve mikser başarısını PII'den arındırıp kolektif AI hafızasına aktarır.
    /// </summary>
    [Authorize]
    [HttpPost("contribute-insight")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ContributeInsight([FromBody] ContributeInsightApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var notes = !string.IsNullOrWhiteSpace(request.DistractionOrLearningNotes)
            ? request.DistractionOrLearningNotes
            : request.Notes;

        var contributed = await Mediator.Send(new ContributeSessionInsightCommand(
            userId,
            request.SessionId,
            request.PlannedDurationMinutes,
            request.FocusQuality,
            notes), cancellationToken);

        return Ok(new { contributed });
    }
}

public record SaveAtmosphereApiRequest(GeneratedAtmosphereDto Atmosphere, string? CustomName = null, string? Name = null);
public record ContributeInsightApiRequest(
    Guid? SessionId = null,
    int? PlannedDurationMinutes = null,
    int? FocusQuality = null,
    string? DistractionOrLearningNotes = null,
    string? Notes = null
);
