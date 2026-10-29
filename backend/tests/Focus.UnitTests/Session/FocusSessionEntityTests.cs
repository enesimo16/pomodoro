using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Xunit;

namespace Focus.UnitTests.Session;

public class FocusSessionEntityTests
{
    [Fact]
    public void Start_WithCustomDurations_InitializesCorrectly()
    {
        var userId = Guid.NewGuid();
        var session = FocusSession.Start(
            userId: userId,
            roomId: null,
            kind: SessionKind.Focus,
            focusMinutes: 48,
            breakMinutes: 17,
            currentRound: 1,
            targetRounds: 4,
            themeId: "cozy_rain",
            mixPresetId: "lofi_beats");

        Assert.Equal(userId, session.UserId);
        Assert.Equal(SessionKind.Focus, session.Kind);
        Assert.Equal(SessionStatus.Running, session.Status);
        Assert.Equal(48, session.FocusMinutes);
        Assert.Equal(17, session.BreakMinutes);
        Assert.Equal(48, session.PlannedMinutes);
        Assert.Equal(1, session.CurrentRound);
        Assert.Equal(4, session.TargetRounds);
        Assert.Equal("cozy_rain", session.ThemeId);
        Assert.Equal("lofi_beats", session.MixPresetId);
        Assert.True(session.PlannedEndAt > session.StartedAt);
    }

    [Fact]
    public void Start_ClampsExtremeDurations()
    {
        var session = FocusSession.Start(
            userId: Guid.NewGuid(),
            roomId: null,
            kind: SessionKind.Focus,
            focusMinutes: 300,
            breakMinutes: -5,
            currentRound: 50,
            targetRounds: 50);

        Assert.Equal(180, session.FocusMinutes);
        Assert.Equal(1, session.BreakMinutes);
        Assert.Equal(20, session.TargetRounds);
        Assert.Equal(20, session.CurrentRound);
    }

    [Fact]
    public void Pause_And_Resume_UpdatesStatusAndPlannedEndAt()
    {
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 25, 5);
        var initialPlannedEnd = session.PlannedEndAt;

        session.Pause();
        Assert.Equal(SessionStatus.Paused, session.Status);
        Assert.NotNull(session.PausedAt);

        // Resume session
        session.Resume();
        Assert.Equal(SessionStatus.Running, session.Status);
        Assert.Null(session.PausedAt);
        Assert.True(session.PlannedEndAt >= initialPlannedEnd);
    }

    [Fact]
    public void Extend_IncreasesPlannedMinutesAndCount()
    {
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 25, 5);
        var initialPlannedMinutes = session.PlannedMinutes;

        session.Extend(10);

        Assert.Equal(initialPlannedMinutes + 10, session.PlannedMinutes);
        Assert.Equal(1, session.ExtensionCount);
    }

    [Fact]
    public void Complete_SetsStatusAndRewards()
    {
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 25, 5);

        session.Complete(netSeconds: 1500, xp: 250, coins: 25);

        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.NotNull(session.EndedAt);
        Assert.Equal(1500, session.NetDurationSeconds);
        Assert.Equal(250, session.XpEarned);
        Assert.Equal(25, session.CoinsEarned);
    }

    [Fact]
    public void Abandon_SetsStatusAndNetDuration()
    {
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 25, 5);

        session.Abandon(netSeconds: 600, xp: 0, coins: 0);

        Assert.Equal(SessionStatus.Abandoned, session.Status);
        Assert.NotNull(session.EndedAt);
        Assert.Equal(600, session.NetDurationSeconds);
        Assert.Equal(0, session.XpEarned);
    }

    [Theory]
    [InlineData(SessionKind.Focus, 1, 4, SessionKind.Break, 1, false)]
    [InlineData(SessionKind.Break, 1, 4, SessionKind.Focus, 2, false)]
    [InlineData(SessionKind.Focus, 4, 4, SessionKind.Break, 4, false)]
    [InlineData(SessionKind.Break, 4, 4, SessionKind.Focus, 1, true)]
    public void GetNextCycleStep_TransitionsCorrectly(
        SessionKind currentKind,
        int currentRound,
        int targetRounds,
        SessionKind expectedNextKind,
        int expectedNextRound,
        bool expectedIsFinished)
    {
        var session = FocusSession.Start(
            Guid.NewGuid(),
            null,
            currentKind,
            focusMinutes: 48,
            breakMinutes: 17,
            currentRound: currentRound,
            targetRounds: targetRounds);

        var (nextKind, nextRound, isFinished) = session.GetNextCycleStep();

        Assert.Equal(expectedNextKind, nextKind);
        Assert.Equal(expectedNextRound, nextRound);
        Assert.Equal(expectedIsFinished, isFinished);
    }

    [Fact]
    public void Extend_StopsAfterMaxExtensions()
    {
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 25, 5);

        Assert.Equal(ExtendOutcome.Extended, session.Extend(10));
        Assert.Equal(ExtendOutcome.Extended, session.Extend(10));
        Assert.Equal(ExtendOutcome.Extended, session.Extend(10));
        Assert.Equal(ExtendOutcome.ExtensionLimitReached, session.Extend(10));
        Assert.Equal(3, session.ExtensionCount);
        Assert.Equal(55, session.PlannedMinutes);
    }

    [Fact]
    public void Extend_ClipsToContinuousFocusLimit_ThenRejects()
    {
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 80, 5);
        var plannedEnd = session.PlannedEndAt;

        Assert.Equal(ExtendOutcome.Extended, session.Extend(30));
        Assert.Equal(90, session.PlannedMinutes);
        Assert.Equal(plannedEnd.AddMinutes(10), session.PlannedEndAt);
        Assert.Equal(ExtendOutcome.ContinuousLimitReached, session.Extend(5));
    }

    [Fact]
    public void Extend_IsNotAllowedOnBreaks_OrFinishedSessions()
    {
        var breakSession = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Break, 25, 5);
        Assert.Equal(ExtendOutcome.NotAllowedForBreak, breakSession.Extend(10));

        var finished = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 25, 5);
        finished.Abandon(0);
        Assert.Equal(ExtendOutcome.NotActive, finished.Extend(10));
    }

    [Fact]
    public void GetNetSeconds_SubtractsPausesAndClampsToPlannedTime()
    {
        var start = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 25, 5, now: start);

        session.Pause(start.AddMinutes(10));
        session.Resume(start.AddMinutes(40));

        Assert.Equal(30 * 60, session.TotalPausedSeconds);
        Assert.Equal(15 * 60, session.GetNetSeconds(start.AddMinutes(45)));
        Assert.Equal(25 * 60, session.GetNetSeconds(start.AddHours(10)));
        Assert.Equal(0, session.GetNetSeconds(start.AddMinutes(-5)));
    }

    [Fact]
    public void GetNetSeconds_WhilePaused_StopsAtThePauseInstant()
    {
        var start = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 25, 5, now: start);

        session.Pause(start.AddMinutes(7));

        Assert.Equal(7 * 60, session.GetNetSeconds(start.AddHours(3)));
    }

    [Fact]
    public void StateChanges_RegenerateTheConcurrencyStamp()
    {
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 25, 5);
        var stamps = new List<Guid> { session.ConcurrencyStamp };

        session.Pause();
        stamps.Add(session.ConcurrencyStamp);
        session.Resume();
        stamps.Add(session.ConcurrencyStamp);
        session.Extend(5);
        stamps.Add(session.ConcurrencyStamp);
        session.Complete(0, 0, 0);
        stamps.Add(session.ConcurrencyStamp);

        Assert.Equal(stamps.Count, stamps.Distinct().Count());
    }
}
