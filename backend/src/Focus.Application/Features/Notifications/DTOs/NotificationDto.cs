using Focus.Domain.Enums;

namespace Focus.Application.Features.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    string? ActionUrl,
    bool IsRead,
    DateTime CreatedAt
);
