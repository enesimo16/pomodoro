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
        user.AddXp(500); // Level 3
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

        var handler = new GetUserFocusStatsQueryHandler(context);
        var stats = await handler.Handle(new GetUserFocusStatsQuery(user.Id), CancellationToken.None);

        Assert.Equal(50, stats.TotalFocusMinutes);
        Assert.Equal(2, stats.TotalCompletedSessions);
        Assert.Equal(50, stats.TodayFocusMinutes);
        Assert.Equal(2, stats.TodaySessionCount);
        Assert.Equal(2, stats.CurrentStreak);
        Assert.Equal(3, stats.CurrentLevel);
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
        // Level 1: 0 to 99 XP. If XP = 50, progress should be 50%
        user.AddXp(50);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetUserFocusStatsQueryHandler(context);
        var stats = await handler.Handle(new GetUserFocusStatsQuery(user.Id), CancellationToken.None);

        Assert.Equal(1, stats.CurrentLevel);
        Assert.Equal(50, stats.CurrentXp);
        Assert.Equal(0, stats.XpRequiredForCurrentLevel);
        Assert.Equal(100, stats.XpRequiredForNextLevel);
        Assert.Equal(50.0, stats.LevelProgressPercentage);
    }
}
