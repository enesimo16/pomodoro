using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Notifications.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Kullanıcının son bildirimlerini (seri uyarıları, ziyaretçi notları, koç tavsiyeleri) tarihe göre sıralı listeler.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _notificationService.GetUserNotificationsAsync(userId, limit, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Belirtilen tekil bildirimi okundu olarak işaretler.
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var updated = await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);
        if (!updated) return NotFound(new { message = "Bildirim bulunamadi veya erisim yetkiniz yok." });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Kullanıcının okunmamış tüm bildirimlerini tek seferde okundu olarak işaretler.
    /// </summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok(new { success = true });
    }
}
