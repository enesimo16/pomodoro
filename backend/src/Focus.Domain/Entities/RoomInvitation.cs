using Focus.Domain.Common;

namespace Focus.Domain.Entities;

public class RoomInvitation : BaseEntity<Guid>
{
    public Guid RoomId { get; private set; }
    public Guid InviterUserId { get; private set; }
    public Guid? InviteeUserId { get; private set; }
    public string? InviteeUsername { get; private set; }
    public string InviteCode { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsAccepted { get; private set; }

    // Navigation
    public StudyRoom Room { get; private set; } = null!;
    public User Inviter { get; private set; } = null!;
    public User? Invitee { get; private set; }

    // EF Core constructor
    private RoomInvitation() { }

    public RoomInvitation(
        Guid roomId,
        Guid inviterUserId,
        string inviteCode,
        Guid? inviteeUserId = null,
        string? inviteeUsername = null,
        int validHours = 48)
    {
        Id = Guid.NewGuid();
        RoomId = roomId;
        InviterUserId = inviterUserId;
        InviteCode = inviteCode;
        InviteeUserId = inviteeUserId;
        InviteeUsername = inviteeUsername;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddHours(validHours);
        IsAccepted = false;
    }

    public void MarkAccepted()
    {
        IsAccepted = true;
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
