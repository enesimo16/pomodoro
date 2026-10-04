using Focus.Domain.Common;

namespace Focus.Domain.Entities;

public class RoomMember : BaseEntity<Guid>
{
    public Guid RoomId { get; private set; }
    public Guid UserId { get; private set; }
    public int? SeatIndex { get; private set; }
    public bool IsFocusing { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LastActiveAt { get; private set; }

    // Navigation
    public StudyRoom Room { get; private set; } = null!;
    public User User { get; private set; } = null!;

    // EF Core constructor
    private RoomMember() { }

    public RoomMember(Guid roomId, Guid userId, int? seatIndex = null)
    {
        Id = Guid.NewGuid();
        RoomId = roomId;
        UserId = userId;
        SeatIndex = seatIndex;
        IsFocusing = false;
        JoinedAt = DateTime.UtcNow;
        LastActiveAt = DateTime.UtcNow;
    }

    public void SitAtSeat(int seatIndex)
    {
        SeatIndex = seatIndex;
        LastActiveAt = DateTime.UtcNow;
    }

    public void LeaveSeat()
    {
        SeatIndex = null;
        IsFocusing = false;
        LastActiveAt = DateTime.UtcNow;
    }

    public void SetFocusing(bool isFocusing)
    {
        IsFocusing = isFocusing;
        LastActiveAt = DateTime.UtcNow;
    }

    public void Touch()
    {
        LastActiveAt = DateTime.UtcNow;
    }
}
