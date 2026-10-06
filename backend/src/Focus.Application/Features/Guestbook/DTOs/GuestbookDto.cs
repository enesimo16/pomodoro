using Focus.Domain.Enums;

namespace Focus.Application.Features.Guestbook.DTOs;

public record GuestbookEntryDto(
    Guid Id,
    string StudyRoomCode,
    Guid SenderUserId,
    string SenderDisplayName,
    string Message,
    GiftType GiftType,
    DateTime CreatedAt
);

public record CreateGuestbookEntryRequest(
    string Message,
    GiftType GiftType = GiftType.None
);
