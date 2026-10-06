using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Notifications.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;

    public NotificationService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var list = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.ActionUrl,
                n.IsRead,
                n.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<bool> MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (item == null)
            return false;

        item.MarkAsRead();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
            return;

        foreach (var item in unread)
        {
            item.MarkAsRead();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationDto> CreateNotificationAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(userId, type, title, message, actionUrl);
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ActionUrl,
            notification.IsRead,
            notification.CreatedAt
        );
    }
}
