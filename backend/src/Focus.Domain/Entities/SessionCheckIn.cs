namespace Focus.Domain.Entities;

using Focus.Domain.Common;
using Focus.Domain.Enums;

public class SessionCheckIn : BaseEntity<Guid>
{
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public SessionMood Mood { get; private set; }
    public int EnergyLevel { get; private set; }
    public string? TargetIntent { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SessionCheckIn() { }

    public SessionCheckIn(Guid sessionId, Guid userId, SessionMood mood, int energyLevel, string? targetIntent)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        UserId = userId;
        Mood = mood;
        EnergyLevel = Math.Clamp(energyLevel, 1, 5);
        TargetIntent = string.IsNullOrWhiteSpace(targetIntent) ? null : targetIntent.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
