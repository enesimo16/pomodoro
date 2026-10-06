using Focus.Application.Features.Auth.DTOs;
using Focus.Application.Features.Avatar.Commands;
using Focus.Application.Features.Avatar.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

public record UpdateAvatarRequest(
    string? SkinTone,
    string? HairStyle,
    string? HairColor,
    string? TopItemId,
    string? BottomItemId,
    string? HatItemId,
    string? GlassesItemId,
    string? ShoesItemId,
    bool ClearHat = false,
    bool ClearGlasses = false);

[Authorize]
public class AvatarController : BaseApiController
{
    /// <summary>
    /// Giriş yapmış kullanıcının piksel avatarının güncel saç, kıyafet ve aksesuar katmanlarını getirir.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AvatarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyAvatar(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var avatar = await Mediator.Send(new GetAvatarQuery(userId), cancellationToken);
        if (avatar == null) return NotFound();

        return Ok(avatar);
    }

    /// <summary>
    /// Piksel avatarın ten rengini, saç stilini/rengini ve giyilen kıyafet/aksesuar parçalarını günceller.
    /// </summary>
    [HttpPatch]
    [ProducesResponseType(typeof(AvatarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAppearance([FromBody] UpdateAvatarRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var command = new UpdateAvatarAppearanceCommand(
            userId,
            request.SkinTone,
            request.HairStyle,
            request.HairColor,
            request.TopItemId,
            request.BottomItemId,
            request.HatItemId,
            request.GlassesItemId,
            request.ShoesItemId,
            request.ClearHat,
            request.ClearGlasses);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
