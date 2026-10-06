using Focus.Application.Features.Notifications.DTOs;
using Focus.Domain.Enums;

namespace Focus.Application.Common.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<NotificationDto> CreateNotificationAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? actionUrl = null,
        CancellationToken cancellationToken = default);
}
