using Focus.Admin.Pages.AiCoach;
using Focus.Domain.Enums;
using Xunit;

namespace Focus.UnitTests.Admin;

public class GlobalInsightsTests
{
    private static GlobalModel.SessionSample Sample(int minutes, SessionStatus status, DateTime startedAtUtc) =>
        new(minutes, status, startedAtUtc);

    [Fact]
    public void CircadianStats_BucketsByIstanbulTimeNotUtc()
    {
        // 21:30 UTC = 00:30 Istanbul, gece dilimine duser
        var sessions = new[] { Sample(25, SessionStatus.Completed, new DateTime(2026, 10, 6, 21, 30, 0, DateTimeKind.Utc)) };

        var stats = GlobalModel.BuildCircadianStats(sessions);

        var night = Assert.Single(stats, s => s.TimeSlot.StartsWith("Gece", StringComparison.Ordinal));
        Assert.Equal(1, night.SessionCount);
        Assert.Equal(100.0, night.CompletionRate);
        Assert.Equal(0, stats.Single(s => s.TimeSlot.StartsWith("Akşam", StringComparison.Ordinal)).SessionCount);
    }

    [Fact]
    public void DurationStats_ComputesCompletionAndAbandonRates()
    {
        var now = DateTime.UtcNow;
        var sessions = new[]
        {
            Sample(25, SessionStatus.Completed, now),
            Sample(25, SessionStatus.Abandoned, now),
            Sample(60, SessionStatus.Completed, now)
        };

        var stats = GlobalModel.BuildDurationStats(sessions);

        var pomodoro = stats.Single(s => s.Label.StartsWith("Standart", StringComparison.Ordinal));
        Assert.Equal(2, pomodoro.SessionCount);
        Assert.Equal(50.0, pomodoro.CompletionRate);
        Assert.Equal(50.0, pomodoro.AbandonRate);
        Assert.DoesNotContain(stats, s => s.Label.StartsWith("Derin", StringComparison.Ordinal));
    }

    [Fact]
    public void Insights_AreEmptyWithoutSessions()
    {
        var insights = GlobalModel.BuildCollectiveInsights(0, [], [], []);

        Assert.Empty(insights);
    }

    [Fact]
    public void Insights_AreDerivedFromTheSuppliedData()
    {
        var durations = new List<GlobalModel.DurationStatItem> { new("Derin Seans (26-45 dk)", 10, 80.0, 20.0) };
        var circadian = new List<GlobalModel.CircadianStatItem> { new("Sabah (06:00 - 12:00)", 10, 100.0, 80.0) };

        var insights = GlobalModel.BuildCollectiveInsights(10, durations, circadian, []);

        Assert.Contains(insights, i => i.Finding.Contains($"%{80.0:F1}", StringComparison.Ordinal) && i.Finding.Contains("Derin Seans", StringComparison.Ordinal));
        Assert.Contains(insights, i => i.Title == "Genel Terk Oranı" && i.Finding.Contains($"%{20.0:F1}", StringComparison.Ordinal));
        Assert.All(insights, i => Assert.StartsWith("Düşük", i.Reliability, StringComparison.Ordinal));
    }
}
