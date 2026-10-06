using Focus.Application.Features.Guestbook.DTOs;
using Focus.Domain.Enums;

namespace Focus.Application.Common.Interfaces;

public interface IGuestbookService
{
    Task<GuestbookEntryDto> AddEntryAsync(
        string roomCode,
        Guid senderUserId,
        string message,
        GiftType giftType = GiftType.None,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GuestbookEntryDto>> GetEntriesByRoomCodeAsync(
        string roomCode,
        int limit = 30,
        CancellationToken cancellationToken = default);
}
