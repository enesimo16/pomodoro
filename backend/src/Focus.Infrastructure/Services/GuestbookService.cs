using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Guestbook.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Services;

public class GuestbookService : IGuestbookService
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public GuestbookService(IApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<GuestbookEntryDto> AddEntryAsync(
        string roomCode,
        Guid senderUserId,
        string message,
        GiftType giftType = GiftType.None,
        CancellationToken cancellationToken = default)
    {
        var room = await _context.StudyRooms
            .FirstOrDefaultAsync(r => r.Code == roomCode, cancellationToken)
            ?? throw new InvalidOperationException($"'{roomCode}' kodlu calisma odasi bulunamadi.");

        var sender = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == senderUserId, cancellationToken)
            ?? throw new InvalidOperationException("Gonderen kullanici bulunamadi.");

        var entry = RoomGuestbookEntry.Create(
            room.Id,
            senderUserId,
            sender.DisplayName,
            message,
            giftType);

        _context.RoomGuestbookEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        // Oda sahibine bildirim gönder (kendi kendine yazmadıysa)
        if (room.OwnerUserId.HasValue && room.OwnerUserId.Value != senderUserId)
        {
            var giftNotice = giftType != GiftType.None ? $" ({giftType})" : string.Empty;
            await _notificationService.CreateNotificationAsync(
                room.OwnerUserId.Value,
                NotificationType.GuestbookNote,
                "Yeni Ziyaretci Notu",
                $"{sender.DisplayName} odaniza bir not birakti{giftNotice}: \"{message}\"",
                $"/rooms/{room.Code}",
                cancellationToken);
        }

        return new GuestbookEntryDto(
            entry.Id,
            room.Code,
            entry.SenderUserId,
            entry.SenderDisplayName,
            entry.Message,
            entry.GiftType,
            entry.CreatedAt
        );
    }

    public async Task<IReadOnlyList<GuestbookEntryDto>> GetEntriesByRoomCodeAsync(
        string roomCode,
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var room = await _context.StudyRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Code == roomCode, cancellationToken);

        if (room == null)
            return Array.Empty<GuestbookEntryDto>();

        var entries = await _context.RoomGuestbookEntries
            .AsNoTracking()
            .Where(e => e.StudyRoomId == room.Id)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
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

        return entries;
    }
}
