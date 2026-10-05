using FluentAssertions;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Infrastructure.Services;
using Xunit;

namespace Focus.UnitTests.Coach;

public class FocusCoachEngineTests
{
    private readonly FocusCoachEngine _engine = new();

    [Fact]
    public void CalculateDailyFatigueScore_WithNoSessions_ReturnsZero()
    {
        var score = _engine.CalculateDailyFatigueScore(new List<FocusSession>());
        score.Should().Be(0);
    }

    [Fact]
    public void CalculateDailyFatigueScore_WithHeavyContinuousSession_ReturnsHighFatigue()
    {
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 120, 15);
        session.Complete(netSeconds: 120 * 60, xp: 1200, coins: 120);

        var score = _engine.CalculateDailyFatigueScore(new List<FocusSession> { session });
        score.Should().BeGreaterThan(30);
    }

    [Fact]
    public void EvaluateAnomalies_WhenLastSessionOlderThan3Days_DetectsAbsenceReturn()
    {
        var user = User.CreateGuest("TestGuest", "avatar_1");
        var oldDate = DateTime.UtcNow.AddDays(-4);

        var oldSession = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5);
        typeof(FocusSession).GetProperty(nameof(FocusSession.StartedAt))!
            .SetValue(oldSession, oldDate);

        var result = _engine.EvaluateAnomalies(
            user,
            new List<FocusSession> { oldSession },
            new List<SessionCheckIn>(),
            new List<SessionReflection>(),
            null);

        result.HasAnomaly.Should().BeTrue();
        result.AnomalyType.Should().Be(AnomalyType.AbsenceReturn);
        result.StatusSummary.Should().Contain("Geri donus");
    }

    [Fact]
    public void EvaluateAnomalies_WhenWeeklyMinutesExceedsThreshold_DetectsWeeklyExcessiveLoad()
    {
        var user = User.CreateGuest("HardWorker", "avatar_1");
        var sessions = new List<FocusSession>();

        // 36 saat (2160 dk) seans simule et
        for (int i = 0; i < 18; i++)
        {
            var s = FocusSession.Start(user.Id, null, SessionKind.Focus, 120, 15);
            s.Complete(netSeconds: 120 * 60, xp: 1200, coins: 120);
            sessions.Add(s);
        }

        var result = _engine.EvaluateAnomalies(
            user,
            sessions,
            new List<SessionCheckIn>(),
            new List<SessionReflection>(),
            null);

        result.HasAnomaly.Should().BeTrue();
        result.AnomalyType.Should().Be(AnomalyType.WeeklyExcessiveLoad);
        result.RecommendedAction.Should().Contain("toparlanma");
    }

    [Fact]
    public void EvaluateAnomalies_WhenRecentReflectionsPoor_DetectsEmotionalStress()
    {
        var user = User.CreateGuest("StressedUser", "avatar_1");
        var sessionId = Guid.NewGuid();

        var reflections = new List<SessionReflection>
        {
            new(sessionId, user.Id, 1, SessionMoodAfter.Frustrated, "Cok zordu"),
            new(sessionId, user.Id, 2, SessionMoodAfter.Exhausted, "Odaklanamadim")
        };

        var result = _engine.EvaluateAnomalies(
            user,
            new List<FocusSession>(),
            new List<SessionCheckIn>(),
            reflections,
            null);

        result.HasAnomaly.Should().BeTrue();
        result.AnomalyType.Should().Be(AnomalyType.EmotionalStress);
    }
}
