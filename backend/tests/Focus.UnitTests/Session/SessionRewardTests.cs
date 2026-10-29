using Focus.Application.Common.Exceptions;
using Focus.Application.Features.Session.Commands;
using Focus.Application.Features.Session.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Focus.UnitTests.Session;

public class SessionRewardTests
{
    private static async Task<(SessionTestKit Kit, User User)> CreateKitWithUserAsync()
    {
        var context = TestDbContextFactory.Create();
        var kit = new SessionTestKit(context);
        var user = User.CreateGuest("RewardUser");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return (kit, user);
    }

    private static async Task<Guid> StartAsync(SessionTestKit kit, User user, SessionKind kind, int minutes)
    {
        var dto = await kit.StartHandler.Handle(
            new StartSessionCommand(user.Id, kind, FocusMinutes: minutes, BreakMinutes: minutes),
            CancellationToken.None);
        return dto.Id;
    }

    private static Task<SessionCompletionResultDto?> CompleteAsync(SessionTestKit kit, User user, Guid sessionId)
        => kit.CompleteHandler.Handle(new CompleteSessionCommand(user.Id, sessionId), CancellationToken.None);

    [Fact]
    public async Task CompleteFocus_AfterFullPlannedTime_PaysTenXpAndOneCoinPerMinute()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(25));

        var result = await CompleteAsync(kit, user, id);

        Assert.NotNull(result);
        Assert.Equal("Completed", result.Session.Status);
        Assert.Equal(250, result.XpEarned);
        Assert.Equal(25, result.CoinsEarned);
        Assert.Equal(25 * 60, result.Session.NetDurationSeconds);
        Assert.Equal("Break", result.NextKind);
        var entry = Assert.Single(context.CoinLedgerEntries.Where(c => c.UserId == user.Id && c.Reason == CoinTransactionReason.SessionReward));
        Assert.Equal(25, entry.Coins);
        Assert.Equal(id, entry.SessionId);
        Assert.Equal(1, (await context.UserStreaks.SingleAsync(s => s.UserId == user.Id)).CurrentStreak);
    }

    [Fact]
    public async Task CompleteFocus_Immediately_PaysNothingAndRecordsNoStreak()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);

        var result = await CompleteAsync(kit, user, id);

        Assert.NotNull(result);
        Assert.Equal(0, result.XpEarned);
        Assert.Equal(0, result.CoinsEarned);
        Assert.Empty(context.CoinLedgerEntries.Where(c => c.UserId == user.Id));
        Assert.Null((await context.UserStreaks.SingleAsync(s => s.UserId == user.Id)).LastActivityDate);
        Assert.Equal(0, (await context.Users.SingleAsync(u => u.Id == user.Id)).CurrentXp);
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(5, 5)]
    [InlineData(12, 12)]
    public async Task CompleteFocus_RespectsMinimumRewardedMinutes(int minutesWorked, int expectedCoins)
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(minutesWorked));

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(expectedCoins, result!.CoinsEarned);
        Assert.Equal(expectedCoins * 10, result.XpEarned);
        Assert.Equal(expectedCoins > 0, (await context.UserStreaks.SingleAsync(s => s.UserId == user.Id)).LastActivityDate.HasValue);
    }

    [Fact]
    public async Task CompleteFocus_NetTimeIsClampedToPlannedMinutes()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromHours(5));

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(25, result!.CoinsEarned);
        Assert.Equal(250, result.XpEarned);
        Assert.Equal(25 * 60, result.Session.NetDurationSeconds);
    }

    [Fact]
    public async Task CompleteFocus_ToleratesSmallClockDriftAgainstThePlannedEnd()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(25) - TimeSpan.FromSeconds(3));

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(25, result!.CoinsEarned);
    }

    [Fact]
    public async Task CompleteFocus_ExcludesPausedTimeFromNetDuration()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(10));
        await kit.PauseHandler.Handle(new PauseSessionCommand(user.Id, id), CancellationToken.None);
        kit.Clock.Advance(TimeSpan.FromMinutes(30));
        await kit.ResumeHandler.Handle(new ResumeSessionCommand(user.Id, id), CancellationToken.None);
        kit.Clock.Advance(TimeSpan.FromMinutes(15));

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(25 * 60, result!.Session.NetDurationSeconds);
        Assert.Equal(25, result.CoinsEarned);
    }

    [Fact]
    public async Task CompleteFocus_WhilePaused_UsesNetTimeUpToThePause()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(8));
        await kit.PauseHandler.Handle(new PauseSessionCommand(user.Id, id), CancellationToken.None);
        kit.Clock.Advance(TimeSpan.FromHours(2));

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(8 * 60, result!.Session.NetDurationSeconds);
        Assert.Equal(8, result.CoinsEarned);
    }

    [Fact]
    public async Task CompleteFocus_AppliesDailyCoinCap_ButStillGrantsXp()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 390, createdAt: kit.Clock.UtcNow.AddHours(-2)));
        await context.SaveChangesAsync();
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(25));

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(10, result!.CoinsEarned);
        Assert.Equal(250, result.XpEarned);
        Assert.Equal(400, result.TotalCoins);
    }

    [Fact]
    public async Task CompleteFocus_AfterCapReached_GrantsNoCoinsAndNoLedgerRow()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 400, createdAt: kit.Clock.UtcNow.AddHours(-1)));
        await context.SaveChangesAsync();
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(25));

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(0, result!.CoinsEarned);
        Assert.Equal(250, result.XpEarned);
        Assert.Single(context.CoinLedgerEntries.Where(c => c.UserId == user.Id && c.Reason == CoinTransactionReason.SessionReward));
    }

    [Fact]
    public async Task CompleteFocus_DailyCapIgnoresRewardsFromPreviousDays()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 400, createdAt: kit.Clock.UtcNow.AddDays(-1)));
        await context.SaveChangesAsync();
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(25));

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(25, result!.CoinsEarned);
    }

    [Fact]
    public async Task CompleteFocus_AppliesStreakMultiplierAndMilestoneBonus()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var streak = UserStreak.CreateDefault(user.Id);
        for (var day = 6; day >= 1; day--)
        {
            streak.RecordActivity(kit.Clock.UtcNow.AddDays(-day));
        }

        context.UserStreaks.Add(streak);
        await context.SaveChangesAsync();
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(25));

        var result = await CompleteAsync(kit, user, id);

        // 7. gun: x1.25 carpan ve 50 coin seri bonusu
        Assert.Equal(312, result!.XpEarned);
        Assert.Equal(31, result.CoinsEarned);
        Assert.Equal(7, streak.CurrentStreak);
        var bonus = Assert.Single(context.CoinLedgerEntries.Where(c => c.UserId == user.Id && c.Reason == CoinTransactionReason.StreakReward));
        Assert.Equal(50, bonus.Coins);
        Assert.Equal(31 + 50, result.TotalCoins);
    }

    [Fact]
    public async Task CompleteFocus_LevelUpIsReported()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 60);
        kit.Clock.Advance(TimeSpan.FromMinutes(60));

        var result = await CompleteAsync(kit, user, id);

        // 600 XP, Level 2 esigi 455
        Assert.True(result!.LevelUp);
        Assert.Equal(2, result.NewLevel);
        Assert.Equal(600, result.TotalXp);
    }

    [Fact]
    public async Task CompleteFocus_WithCoWorkingFriend_AddsFifteenPercent_AndPresenceIsToggled()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var friend = User.CreateGuest("Friend");
        context.Users.Add(friend);
        var room = StudyRoom.CreatePublicLibrary("Library", "LIB-1", "Test", capacity: 10);
        var myMember = room.AddMember(user.Id, 1);
        var friendMember = room.AddMember(friend.Id, 2);
        friendMember.SetFocusing(true);
        context.StudyRooms.Add(room);
        await context.SaveChangesAsync();

        var id = await StartAsync(kit, user, SessionKind.Focus, 20);
        Assert.True(myMember.IsFocusing);
        kit.Clock.Advance(TimeSpan.FromMinutes(20));

        var result = await CompleteAsync(kit, user, id);

        // 200 XP ve 20 coin x1.15
        Assert.Equal(230, result!.XpEarned);
        Assert.Equal(23, result.CoinsEarned);
        Assert.False(myMember.IsFocusing);
        Assert.True(friendMember.IsFocusing);
    }

    [Fact]
    public async Task CompleteFocus_WhenFriendIsNotFocusing_HasNoBonus()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var friend = User.CreateGuest("IdleFriend");
        context.Users.Add(friend);
        var room = StudyRoom.CreatePublicLibrary("Library", "LIB-2", "Test", capacity: 10);
        room.AddMember(user.Id, 1);
        room.AddMember(friend.Id, 2);
        context.StudyRooms.Add(room);
        await context.SaveChangesAsync();

        var id = await StartAsync(kit, user, SessionKind.Focus, 20);
        kit.Clock.Advance(TimeSpan.FromMinutes(20));

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(200, result!.XpEarned);
        Assert.Equal(20, result.CoinsEarned);
    }

    [Fact]
    public async Task Presence_FollowsPauseResumeAndAbandon_ForTheMostRecentMembership()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var olderRoom = StudyRoom.CreatePublicLibrary("Old", "LIB-OLD", "Test", capacity: 10);
        var newerRoom = StudyRoom.CreatePublicLibrary("New", "LIB-NEW", "Test", capacity: 10);
        var older = olderRoom.AddMember(user.Id, 1);
        var newer = newerRoom.AddMember(user.Id, 1);
        context.StudyRooms.AddRange(olderRoom, newerRoom);
        await context.SaveChangesAsync();
        kit.Clock.Advance(TimeSpan.FromMinutes(1));
        newer.Touch();
        await context.SaveChangesAsync();

        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        Assert.True(newer.IsFocusing);
        Assert.False(older.IsFocusing);

        await kit.PauseHandler.Handle(new PauseSessionCommand(user.Id, id), CancellationToken.None);
        Assert.False(newer.IsFocusing);

        await kit.ResumeHandler.Handle(new ResumeSessionCommand(user.Id, id), CancellationToken.None);
        Assert.True(newer.IsFocusing);

        await kit.AbandonHandler.Handle(new AbandonSessionCommand(user.Id, id), CancellationToken.None);
        Assert.False(newer.IsFocusing);
    }

    [Fact]
    public async Task CompleteBreak_Immediately_PaysNothing()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Break, 5);

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(0, result!.XpEarned);
        Assert.Equal(0, result.CoinsEarned);
        Assert.Empty(context.CoinLedgerEntries.Where(c => c.UserId == user.Id));
    }

    [Fact]
    public async Task CompleteBreak_WithoutPrecedingCompletedFocus_PaysNothingEvenWhenFullyServed()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Break, 5);
        kit.Clock.Advance(TimeSpan.FromMinutes(5));

        var result = await CompleteAsync(kit, user, id);

        Assert.Equal(0, result!.XpEarned);
        Assert.Equal(0, result.CoinsEarned);
    }

    [Fact]
    public async Task CompleteBreak_AfterFocusAndFullBreak_PaysBreakBonusOnce()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        await RunRewardedFocusAsync(kit, user);

        var breakId = await StartAsync(kit, user, SessionKind.Break, 5);
        kit.Clock.Advance(TimeSpan.FromMinutes(5));
        var result = await CompleteAsync(kit, user, breakId);

        Assert.Equal(50, result!.XpEarned);
        Assert.Equal(10, result.CoinsEarned);
        Assert.Single(context.CoinLedgerEntries.Where(c => c.UserId == user.Id && c.Reason == CoinTransactionReason.BreakBonus));

        // Ayni odak seansi icin ikinci mola odul vermez
        var secondBreak = await StartAsync(kit, user, SessionKind.Break, 5);
        kit.Clock.Advance(TimeSpan.FromMinutes(5));
        var second = await CompleteAsync(kit, user, secondBreak);

        Assert.Equal(0, second!.XpEarned);
        Assert.Equal(0, second.CoinsEarned);
    }

    [Fact]
    public async Task CompleteBreak_ServedBelowNinetyPercent_PaysNothing()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        await RunRewardedFocusAsync(kit, user);

        var breakId = await StartAsync(kit, user, SessionKind.Break, 10);
        kit.Clock.Advance(TimeSpan.FromMinutes(8));
        var result = await CompleteAsync(kit, user, breakId);

        Assert.Equal(0, result!.CoinsEarned);
    }

    [Fact]
    public async Task CompleteBreak_StartedLongAfterTheFocusEnded_PaysNothing()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        await RunRewardedFocusAsync(kit, user);
        kit.Clock.Advance(TimeSpan.FromMinutes(30));

        var breakId = await StartAsync(kit, user, SessionKind.Break, 5);
        kit.Clock.Advance(TimeSpan.FromMinutes(5));
        var result = await CompleteAsync(kit, user, breakId);

        Assert.Equal(0, result!.CoinsEarned);
    }

    [Fact]
    public async Task CompleteBreak_AfterAFocusThatEarnedNothing_PaysNothing()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var focusId = await StartAsync(kit, user, SessionKind.Focus, 25);
        await CompleteAsync(kit, user, focusId);

        var breakId = await StartAsync(kit, user, SessionKind.Break, 5);
        kit.Clock.Advance(TimeSpan.FromMinutes(5));
        var result = await CompleteAsync(kit, user, breakId);

        Assert.Equal(0, result!.CoinsEarned);
        Assert.Equal(0, result.XpEarned);
    }

    [Fact]
    public async Task CompleteSession_WhenAlreadyCompleted_ReturnsNullAndDoesNotPayTwice()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(25));

        var first = await CompleteAsync(kit, user, id);
        var second = await CompleteAsync(kit, user, id);

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Single(context.CoinLedgerEntries.Where(c => c.UserId == user.Id && c.Reason == CoinTransactionReason.SessionReward));
    }

    [Fact]
    public async Task CompleteSession_WhenAnotherRequestCompletedItFirst_ThrowsConflict()
    {
        var dbName = Guid.NewGuid().ToString();
        using var seed = TestDbContextFactory.CreateShared(dbName);
        var clock = new TestClock();
        var user = User.CreateGuest("Racer");
        seed.Users.Add(user);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: clock.UtcNow);
        seed.FocusSessions.Add(session);
        await seed.SaveChangesAsync();
        clock.Advance(TimeSpan.FromMinutes(25));

        using var first = TestDbContextFactory.CreateShared(dbName);
        using var second = TestDbContextFactory.CreateThrowingOnSave(dbName);
        var firstKit = new SessionTestKit(first, clock);
        var secondKit = new SessionTestKit(second, clock);
        second.BeforeSave = () => firstKit.CompleteHandler.Handle(new CompleteSessionCommand(user.Id, session.Id), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            secondKit.CompleteHandler.Handle(new CompleteSessionCommand(user.Id, session.Id), CancellationToken.None));

        using var verify = TestDbContextFactory.CreateShared(dbName);
        Assert.Equal(SessionStatus.Completed, (await verify.FocusSessions.SingleAsync(s => s.Id == session.Id)).Status);
        Assert.Equal(250, (await verify.Users.SingleAsync(u => u.Id == user.Id)).CurrentXp);
    }

    [Fact]
    public async Task AbandonFocus_BelowTenMinutes_PaysNothing()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(9));

        var abandoned = await kit.AbandonHandler.Handle(new AbandonSessionCommand(user.Id, id), CancellationToken.None);

        Assert.Equal("Abandoned", abandoned!.Status);
        Assert.Equal(9 * 60, abandoned.NetDurationSeconds);
        Assert.Equal(0, abandoned.CoinsEarned);
        Assert.Empty(context.CoinLedgerEntries.Where(c => c.UserId == user.Id));
        Assert.Null((await context.UserStreaks.SingleAsync(s => s.UserId == user.Id)).LastActivityDate);
    }

    [Fact]
    public async Task AbandonFocus_AboveTenMinutes_PaysNetMinutesWithoutStreak()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(12));

        var abandoned = await kit.AbandonHandler.Handle(new AbandonSessionCommand(user.Id, id), CancellationToken.None);

        Assert.Equal(12, abandoned!.CoinsEarned);
        Assert.Equal(120, abandoned.XpEarned);
        Assert.Single(context.CoinLedgerEntries.Where(c => c.UserId == user.Id && c.Reason == CoinTransactionReason.SessionReward));
        Assert.Null((await context.UserStreaks.SingleAsync(s => s.UserId == user.Id)).LastActivityDate);
    }

    [Fact]
    public async Task AbandonBreak_NeverPays()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var id = await StartAsync(kit, user, SessionKind.Break, 15);
        kit.Clock.Advance(TimeSpan.FromMinutes(14));

        var abandoned = await kit.AbandonHandler.Handle(new AbandonSessionCommand(user.Id, id), CancellationToken.None);

        Assert.Equal(0, abandoned!.CoinsEarned);
        Assert.Empty(context.CoinLedgerEntries.Where(c => c.UserId == user.Id));
    }

    private static async Task RunRewardedFocusAsync(SessionTestKit kit, User user)
    {
        var focusId = await StartAsync(kit, user, SessionKind.Focus, 25);
        kit.Clock.Advance(TimeSpan.FromMinutes(25));
        var focus = await CompleteAsync(kit, user, focusId);
        Assert.True(focus!.XpEarned > 0);
    }
}
