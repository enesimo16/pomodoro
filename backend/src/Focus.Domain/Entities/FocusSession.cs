using Focus.Domain.Common;
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public enum ExtendOutcome
{
    Extended = 0,
    NotActive = 1,
    NotAllowedForBreak = 2,
    ExtensionLimitReached = 3,
    ContinuousLimitReached = 4
}

public class FocusSession : BaseEntity<Guid>
{
    public const int MinMinutes = 1;
    public const int MaxFocusMinutes = 180;
    public const int MaxBreakMinutes = 60;
    public const int MaxRounds = 20;
    public const int MaxExtensionMinutes = 30;
    public const int DefaultMaxExtensions = 3;
    public const int DefaultMaxContinuousMinutes = 90;

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
    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

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
        string? mixPresetId = null,
        DateTime? now = null)
    {
        var validFocus = Math.Clamp(focusMinutes, MinMinutes, MaxFocusMinutes);
        var validBreak = Math.Clamp(breakMinutes, MinMinutes, MaxBreakMinutes);
        var validRounds = Math.Clamp(targetRounds, 1, MaxRounds);
        var validCurrentRound = Math.Clamp(currentRound, 1, validRounds);

        var plannedMinutes = kind == SessionKind.Focus ? validFocus : validBreak;
        var startedAt = now ?? DateTime.UtcNow;

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
            StartedAt = startedAt,
            PlannedEndAt = startedAt.AddMinutes(plannedMinutes),
            TotalPausedSeconds = 0,
            NetDurationSeconds = 0,
            ExtensionCount = 0,
            XpEarned = 0,
            CoinsEarned = 0,
            ThemeId = themeId,
            MixPresetId = mixPresetId
        };
    }

    public bool IsActive => Status == SessionStatus.Running || Status == SessionStatus.Paused;

    public void Pause(DateTime? now = null)
    {
        if (Status != SessionStatus.Running) return;

        Status = SessionStatus.Paused;
        PausedAt = now ?? DateTime.UtcNow;
        Touch();
    }

    public void Resume(DateTime? now = null)
    {
        if (Status != SessionStatus.Paused) return;

        var resumedAt = now ?? DateTime.UtcNow;
        if (PausedAt.HasValue)
        {
            var pauseDuration = Math.Max(0, (int)(resumedAt - PausedAt.Value).TotalSeconds);
            TotalPausedSeconds += pauseDuration;
            PlannedEndAt = PlannedEndAt.AddSeconds(pauseDuration);
        }

        Status = SessionStatus.Running;
        PausedAt = null;
        Touch();
    }

    // Akis Korumasi: yalnizca odak seanslari, en fazla maxExtensions kez ve toplam sure maxContinuousMinutes'i gecmeyecek sekilde uzatilir
    public ExtendOutcome Extend(
        int extraMinutes = 10,
        int maxExtensions = DefaultMaxExtensions,
        int maxContinuousMinutes = DefaultMaxContinuousMinutes)
    {
        if (!IsActive) return ExtendOutcome.NotActive;
        if (Kind != SessionKind.Focus) return ExtendOutcome.NotAllowedForBreak;
        if (ExtensionCount >= maxExtensions) return ExtendOutcome.ExtensionLimitReached;

        var remainingCapacity = maxContinuousMinutes - PlannedMinutes;
        if (remainingCapacity <= 0) return ExtendOutcome.ContinuousLimitReached;

        var validExtra = Math.Min(Math.Clamp(extraMinutes, 1, MaxExtensionMinutes), remainingCapacity);
        PlannedEndAt = PlannedEndAt.AddMinutes(validExtra);
        PlannedMinutes += validExtra;
        ExtensionCount++;
        Touch();
        return ExtendOutcome.Extended;
    }

    // Net odak suresi: duraklatmalar dusulur, planlanan sureyi (uzatmalar dahil) asamaz
    public int GetNetSeconds(DateTime until)
    {
        var activeSeconds = (until - StartedAt).TotalSeconds - TotalPausedSeconds;
        if (Status == SessionStatus.Paused && PausedAt.HasValue)
        {
            activeSeconds -= (until - PausedAt.Value).TotalSeconds;
        }

        return Math.Clamp((int)Math.Floor(activeSeconds), 0, PlannedMinutes * 60);
    }

    public void Complete(int netSeconds, int xp, int coins, DateTime? endedAt = null)
    {
        Status = SessionStatus.Completed;
        EndedAt = endedAt ?? DateTime.UtcNow;
        PausedAt = null;
        NetDurationSeconds = Math.Max(0, netSeconds);
        XpEarned = Math.Max(0, xp);
        CoinsEarned = Math.Max(0, coins);
        Touch();
    }

    public void Abandon(int netSeconds, int xp = 0, int coins = 0, DateTime? endedAt = null)
    {
        Status = SessionStatus.Abandoned;
        EndedAt = endedAt ?? DateTime.UtcNow;
        PausedAt = null;
        NetDurationSeconds = Math.Max(0, netSeconds);
        XpEarned = Math.Max(0, xp);
        CoinsEarned = Math.Max(0, coins);
        Touch();
    }

    private void Touch() => ConcurrencyStamp = Guid.NewGuid();

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
