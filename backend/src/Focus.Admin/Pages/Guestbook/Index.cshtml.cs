using Focus.Application.Common.Interfaces;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.Guestbook;

public class IndexModel : PageModel
{
    private readonly IApplicationDbContext _context;

    public IndexModel(IApplicationDbContext context)
    {
        _context = context;
    }

    public int TotalGuestbookEntries { get; set; }
    public int TotalGifts { get; set; }
    public int TotalNotifications { get; set; }
    public int UnreadNotifications { get; set; }

    public List<GuestbookItem> GuestbookEntries { get; set; } = new();
    public List<NotificationItem> RecentNotifications { get; set; } = new();

    public record GuestbookItem(
        Guid Id,
        string RoomCode,
        string RoomName,
        string AuthorName,
        string Message,
        GiftType Gift,
        DateTime CreatedAt);

    public record NotificationItem(
        Guid Id,
        string TargetUserName,
        string Title,
        string Message,
        NotificationType Type,
        bool IsRead,
        DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        TotalGuestbookEntries = await _context.RoomGuestbookEntries.CountAsync();
        TotalGifts = await _context.RoomGuestbookEntries.CountAsync(g => g.GiftType != GiftType.None);
        TotalNotifications = await _context.Notifications.CountAsync();
        UnreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead);

        var entries = await _context.RoomGuestbookEntries
            .OrderByDescending(g => g.CreatedAt)
            .Take(50)
            .ToListAsync();

        var roomIds = entries.Select(g => g.StudyRoomId).Distinct().ToList();

        var rooms = await _context.StudyRooms
            .Where(r => roomIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => new { r.Code, r.Name });

        GuestbookEntries = entries.Select(g =>
        {
            rooms.TryGetValue(g.StudyRoomId, out var roomInfo);
            return new GuestbookItem(
                g.Id,
                roomInfo?.Code ?? "Bilinmeyen",
                roomInfo?.Name ?? "Oda",
                !string.IsNullOrWhiteSpace(g.SenderDisplayName) ? g.SenderDisplayName : "Misafir",
                g.Message,
                g.GiftType,
                g.CreatedAt);
        }).ToList();

        var notifs = await _context.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(30)
            .ToListAsync();

        var targetUserIds = notifs.Select(n => n.UserId).Distinct().ToList();
        var targetUsers = await _context.Users
            .Where(u => targetUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        RecentNotifications = notifs.Select(n => new NotificationItem(
            n.Id,
            targetUsers.GetValueOrDefault(n.UserId, "Kullanıcı"),
            n.Title,
            n.Message,
            n.Type,
            n.IsRead,
            n.CreatedAt)).ToList();
    }
}
