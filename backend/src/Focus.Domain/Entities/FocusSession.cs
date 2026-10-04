using Focus.Domain.Common;
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public class FocusSession : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid? RoomId { get; private set; }
    public SessionKind Kind { get; private set; }
    public SessionStatus Status { get; private set; }
    public int FocusMinutes { get; private set; }
    public int BreakMinutes { get; private set; }
    public int PlannedMinutes { get; private set; }
    public int CurrentRound { get; private set; }
    public int TargetRounds { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime PlannedEndAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public DateTime? PausedAt { get; private set; }
    public int TotalPausedSeconds { get; private set; }
    public int NetDurationSeconds { get; private set; }
    public int ExtensionCount { get; private set; }
    public int XpEarned { get; private set; }
    public int CoinsEarned { get; private set; }
    public string? ThemeId { get; private set; }
    public string? MixPresetId { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;
    public PixelRoom? Room { get; private set; }

    // EF Core constructor
    private FocusSession() { }

    public static FocusSession Start(
        Guid userId,
        Guid? roomId,
        SessionKind kind,
        int focusMinutes,
        int breakMinutes,
        int currentRound = 1,
        int targetRounds = 4,
        string? themeId = null,
        string? mixPresetId = null)
    {
        var validFocus = Math.Clamp(focusMinutes, 1, 180);
        var validBreak = Math.Clamp(breakMinutes, 1, 60);
        var validRounds = Math.Clamp(targetRounds, 1, 20);
        var validCurrentRound = Math.Clamp(currentRound, 1, validRounds);

        var plannedMinutes = kind == SessionKind.Focus ? validFocus : validBreak;
        var now = DateTime.UtcNow;

        return new FocusSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoomId = roomId,
            Kind = kind,
            Status = SessionStatus.Running,
            FocusMinutes = validFocus,
            BreakMinutes = validBreak,
            PlannedMinutes = plannedMinutes,
            CurrentRound = validCurrentRound,
            TargetRounds = validRounds,
            StartedAt = now,
            PlannedEndAt = now.AddMinutes(plannedMinutes),
            TotalPausedSeconds = 0,
            NetDurationSeconds = 0,
            ExtensionCount = 0,
            XpEarned = 0,
            CoinsEarned = 0,
            ThemeId = themeId,
            MixPresetId = mixPresetId
        };
    }

    public void Pause()
    {
        if (Status != SessionStatus.Running) return;

        Status = SessionStatus.Paused;
        PausedAt = DateTime.UtcNow;
    }

    public void Resume()
    {
        if (Status != SessionStatus.Paused) return;

        var now = DateTime.UtcNow;
        if (PausedAt.HasValue)
        {
            var pauseDuration = (int)(now - PausedAt.Value).TotalSeconds;
            TotalPausedSeconds += Math.Max(0, pauseDuration);
            PlannedEndAt = PlannedEndAt.AddSeconds(pauseDuration);
        }

        Status = SessionStatus.Running;
        PausedAt = null;
    }

    public void Extend(int extraMinutes = 10)
    {
        if (Status != SessionStatus.Running && Status != SessionStatus.Paused) return;

        var validExtra = Math.Clamp(extraMinutes, 1, 30);
        PlannedEndAt = PlannedEndAt.AddMinutes(validExtra);
        PlannedMinutes += validExtra;
        ExtensionCount++;
    }

    public void Complete(int netSeconds, int xp, int coins)
    {
        Status = SessionStatus.Completed;
        EndedAt = DateTime.UtcNow;
        NetDurationSeconds = Math.Max(0, netSeconds);
        XpEarned = Math.Max(0, xp);
        CoinsEarned = Math.Max(0, coins);
    }

    public void Abandon(int netSeconds, int xp = 0, int coins = 0)
    {
        Status = SessionStatus.Abandoned;
        EndedAt = DateTime.UtcNow;
        NetDurationSeconds = Math.Max(0, netSeconds);
        XpEarned = Math.Max(0, xp);
        CoinsEarned = Math.Max(0, coins);
    }

    public (SessionKind NextKind, int NextRound, bool IsCycleFinished) GetNextCycleStep()
    {
        if (Kind == SessionKind.Focus)
        {
            // Odak bitti -> Ayni turun molasina gec
            return (SessionKind.Break, CurrentRound, false);
        }

        // Mola bitti -> Sonraki turun odagina gec veya donguyu bitir
        if (CurrentRound < TargetRounds)
        {
            return (SessionKind.Focus, CurrentRound + 1, false);
        }

        return (SessionKind.Focus, 1, true); // Dongu tamamlandi
    }
}
