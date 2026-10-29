using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Guestbook.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Services;

public class GuestbookService : IGuestbookService
{
    public const int MaxListLimit = 100;
    private const int NotificationPreviewLength = 200;

    private readonly IApplicationDbContext _context;

    public GuestbookService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GuestbookEntryDto> AddEntryAsync(
        string roomCode,
        Guid senderUserId,
        string message,
        GiftType giftType = GiftType.None,
        CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(roomCode);
        var room = await _context.StudyRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken)
            ?? throw new InvalidOperationException("Çalışma odası bulunamadı.");

        if (room.IsPrivate && room.OwnerUserId != senderUserId && !room.HasMember(senderUserId))
        {
            throw new InvalidOperationException("Çalışma odası bulunamadı.");
        }

        var sender = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == senderUserId, cancellationToken)
            ?? throw new InvalidOperationException("Gönderen kullanıcı bulunamadı.");

        var entry = RoomGuestbookEntry.Create(
            room.Id,
            senderUserId,
            sender.DisplayName,
            message,
            giftType);

        _context.RoomGuestbookEntries.Add(entry);

        // Oda sahibine bildirim gonderilir (kendi kendine yazmadiysa); not ile ayni kayit islemiyle saklanir
        if (room.OwnerUserId.HasValue && room.OwnerUserId.Value != senderUserId)
        {
            var giftNotice = giftType != GiftType.None ? $" ({giftType})" : string.Empty;
            var preview = entry.Message.Length > NotificationPreviewLength
                ? entry.Message[..NotificationPreviewLength] + "..."
                : entry.Message;

            _context.Notifications.Add(Notification.Create(
                room.OwnerUserId.Value,
                NotificationType.GuestbookNote,
                "Yeni Ziyaretçi Notu",
                $"{sender.DisplayName} odanıza bir not bıraktı{giftNotice}: \"{preview}\"",
                $"/rooms/{room.Code}"));
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(entry, room.Code);
    }

    public async Task<IReadOnlyList<GuestbookEntryDto>> GetEntriesByRoomCodeAsync(
        string roomCode,
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(roomCode);
        var room = await _context.StudyRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (room == null)
            return Array.Empty<GuestbookEntryDto>();

        return await _context.RoomGuestbookEntries
            .AsNoTracking()
            .Where(e => e.StudyRoomId == room.Id)
            .OrderByDescending(e => e.CreatedAt)
            .Take(Math.Clamp(limit, 1, MaxListLimit))
            .Select(e => new GuestbookEntryDto(
                e.Id,
                room.Code,
                e.SenderUserId,
                e.SenderDisplayName,
                e.Message,
                e.GiftType,
                e.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteEntryAsync(
        string roomCode,
        Guid entryId,
        Guid requesterUserId,
        CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(roomCode);
        var entry = await _context.RoomGuestbookEntries
            .Include(e => e.StudyRoom)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.StudyRoom.Code == code, cancellationToken);

        if (entry == null) return false;

        var isOwner = entry.StudyRoom.OwnerUserId == requesterUserId;
        if (!isOwner && entry.SenderUserId != requesterUserId) return false;

        _context.RoomGuestbookEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeCode(string roomCode) => roomCode.Trim().ToUpperInvariant();

    private static GuestbookEntryDto ToDto(RoomGuestbookEntry entry, string roomCode)
    {
        return new GuestbookEntryDto(
            entry.Id,
            roomCode,
            entry.SenderUserId,
            entry.SenderDisplayName,
            entry.Message,
            entry.GiftType,
            entry.CreatedAt);
    }
}
