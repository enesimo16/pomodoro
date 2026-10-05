namespace Focus.Domain.Entities;

using Focus.Domain.Common;
using Focus.Domain.Enums;

public class SessionReflection : BaseEntity<Guid>
{
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public int FocusQuality { get; private set; }
    public SessionMoodAfter MoodAfter { get; private set; }
    public string? DistractionNote { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SessionReflection() { }

    public SessionReflection(Guid sessionId, Guid userId, int focusQuality, SessionMoodAfter moodAfter, string? distractionNote)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        UserId = userId;
        FocusQuality = Math.Clamp(focusQuality, 1, 5);
        MoodAfter = moodAfter;
        DistractionNote = string.IsNullOrWhiteSpace(distractionNote) ? null : distractionNote.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
