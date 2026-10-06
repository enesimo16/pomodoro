using Focus.Domain.Common;
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public class Notification : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? ActionUrl { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private Notification() { }

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? actionUrl = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title.Trim(),
            Message = message.Trim(),
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }
}
