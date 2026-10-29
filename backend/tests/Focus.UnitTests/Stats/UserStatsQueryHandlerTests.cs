using Focus.Application.Features.Stats.Queries;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Stats;

public class UserStatsQueryHandlerTests
{
    [Fact]
    public async Task GetUserFocusStats_WithCompletedSessions_ShouldCalculateAccurateTotalsAndStreak()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("StatsMaster");
        user.AddXp(500); // Level 2 (455 XP esigi)
        context.Users.Add(user);

        var streak = UserStreak.CreateDefault(user.Id);
        streak.RecordActivity(DateTime.UtcNow.AddDays(-1));
        streak.RecordActivity(DateTime.UtcNow);
        context.UserStreaks.Add(streak);

        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 120));
        context.UserInventoryItems.Add(new UserInventoryItem(user.Id, "plant_bonsai"));

        // Add 2 completed focus sessions: 25 mins each (1500 sec)
        var s1 = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, 1, 4);
        s1.Complete(1500, 250, 25);
        var s2 = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, 2, 4);
        s2.Complete(1500, 250, 25);

        context.FocusSessions.AddRange(s1, s2);
        await context.SaveChangesAsync();

        var handler = new GetUserFocusStatsQueryHandler(context, TimeProvider.System);
        var stats = await handler.Handle(new GetUserFocusStatsQuery(user.Id), CancellationToken.None);

        Assert.Equal(50, stats.TotalFocusMinutes);
        Assert.Equal(2, stats.TotalCompletedSessions);
        Assert.Equal(50, stats.TodayFocusMinutes);
        Assert.Equal(2, stats.TodaySessionCount);
        Assert.Equal(2, stats.CurrentStreak);
        Assert.Equal(2, stats.CurrentLevel);
        Assert.Equal(500, stats.CurrentXp);
        Assert.Equal(120, stats.TotalCoins);
        Assert.Equal(1, stats.InventoryItemCount);
        Assert.Equal(7, stats.Last7Days.Count);
    }

    [Fact]
    public async Task GetUserFocusStats_LevelProgress_ShouldCalculateAccuratePercentage()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("LevelProgressTest");
        // Level 1: 0 - 454 XP, Level 2 esigi 455 XP
        user.AddXp(91);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetUserFocusStatsQueryHandler(context, TimeProvider.System);
        var stats = await handler.Handle(new GetUserFocusStatsQuery(user.Id), CancellationToken.None);

        Assert.Equal(1, stats.CurrentLevel);
        Assert.Equal(91, stats.CurrentXp);
        Assert.Equal(0, stats.XpRequiredForCurrentLevel);
        Assert.Equal(455, stats.XpRequiredForNextLevel);
        Assert.Equal(20.0, stats.LevelProgressPercentage);
    }

    [Fact]
    public async Task GetUserFocusStats_AggregatesPeriodsAndIgnoresBreaksAndAbandonedSessions()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("PeriodUser");
        context.Users.Add(user);

        // Carsamba 14 Ekim 2026 12:00 UTC
        var now = new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);
        context.FocusSessions.AddRange(
            CompletedFocus(user, SessionKind.Focus, new DateTime(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc), 30),
            CompletedFocus(user, SessionKind.Focus, new DateTime(2026, 10, 12, 20, 0, 0, DateTimeKind.Utc), 20),
            CompletedFocus(user, SessionKind.Focus, new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc), 40),
            CompletedFocus(user, SessionKind.Focus, new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc), 50),
            CompletedFocus(user, SessionKind.Break, new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc), 15));

        var abandoned = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: now.AddHours(-3));
        abandoned.Abandon(25 * 60, endedAt: now.AddHours(-2));
        context.FocusSessions.Add(abandoned);
        await context.SaveChangesAsync();

        var handler = new GetUserFocusStatsQueryHandler(context, new TestClock(now));
        var stats = await handler.Handle(new GetUserFocusStatsQuery(user.Id), CancellationToken.None);

        Assert.Equal(140, stats.TotalFocusMinutes);
        Assert.Equal(4, stats.TotalCompletedSessions);
        Assert.Equal(30, stats.TodayFocusMinutes);
        Assert.Equal(1, stats.TodaySessionCount);
        Assert.Equal(50, stats.ThisWeekFocusMinutes);
        Assert.Equal(90, stats.ThisMonthFocusMinutes);
        Assert.Equal(7, stats.Last7Days.Count);
        Assert.Equal(30, stats.Last7Days.Single(d => d.Date.Date == now.Date).Minutes);
        Assert.Equal(20, stats.Last7Days.Single(d => d.Date.Date == new DateTime(2026, 10, 12)).Minutes);
        Assert.Equal(50, stats.Last7Days.Sum(d => d.Minutes));
    }

    [Fact]
    public async Task GetUserFocusStats_WithoutSessions_ReturnsZeros()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("EmptyStats");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetUserFocusStatsQueryHandler(context, new TestClock());
        var stats = await handler.Handle(new GetUserFocusStatsQuery(user.Id), CancellationToken.None);

        Assert.Equal(0, stats.TotalFocusMinutes);
        Assert.Equal(0, stats.TotalCompletedSessions);
        Assert.Equal(0, stats.ThisMonthFocusMinutes);
        Assert.All(stats.Last7Days, d => Assert.Equal(0, d.Minutes));
    }

    private static FocusSession CompletedFocus(User user, SessionKind kind, DateTime endedAt, int minutes)
    {
        var session = FocusSession.Start(user.Id, null, kind, minutes, minutes, now: endedAt.AddMinutes(-minutes));
        session.Complete(minutes * 60, minutes * 10, minutes, endedAt);
        return session;
    }
}
