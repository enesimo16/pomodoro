using Focus.Domain.Common;
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public class RoomGuestbookEntry : BaseEntity<Guid>
{
    public Guid StudyRoomId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string SenderDisplayName { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public GiftType GiftType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public StudyRoom StudyRoom { get; private set; } = null!;
    public User SenderUser { get; private set; } = null!;

    private RoomGuestbookEntry() { }

    public static RoomGuestbookEntry Create(
        Guid studyRoomId,
        Guid senderUserId,
        string senderDisplayName,
        string message,
        GiftType giftType = GiftType.None)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Mesaj bos olamaz.", nameof(message));

        return new RoomGuestbookEntry
        {
            Id = Guid.NewGuid(),
            StudyRoomId = studyRoomId,
            SenderUserId = senderUserId,
            SenderDisplayName = senderDisplayName.Trim(),
            Message = message.Trim(),
            GiftType = giftType,
            CreatedAt = DateTime.UtcNow
        };
    }
}
