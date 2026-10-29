using Focus.Application.Common.Interfaces;
using Focus.Application.Common.Options;
using Focus.Application.Features.Session.Services;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Infrastructure.BackgroundJobs;
using Focus.Infrastructure.Persistence;
using Focus.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Focus.UnitTests.BackgroundJobs;

public class SessionMaintenanceJobTests
{
    private static (BackgroundJobService Service, TestClock Clock, ApplicationDbContext Context) Create()
    {
        var context = TestDbContextFactory.Create();
        var clock = new TestClock();
        var options = Microsoft.Extensions.Options.Options.Create(new AlgorithmOptions());
        var service = new BackgroundJobService(
            context,
            new Mock<IVectorMemoryService>().Object,
            new SessionSettlementService(context, options),
            options,
            clock,
            NullLogger<BackgroundJobService>.Instance);
        return (service, clock, context);
    }

    private static async Task<User> AddUserAsync(ApplicationDbContext context)
    {
        var user = User.CreateGuest("JobUser");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Reconciliation_CompletesRunningSessionPastGraceWithPlannedTimeRewards()
    {
        var (service, clock, context) = Create();
        using var _ = context;
        var user = await AddUserAsync(context);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();
        clock.Advance(TimeSpan.FromMinutes(25 + 11));

        var count = await service.RunAbandonedSessionReconciliationAsync();

        Assert.Equal(1, count);
        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.Equal(25 * 60, session.NetDurationSeconds);
        Assert.Equal(250, session.XpEarned);
        Assert.Equal(25, session.CoinsEarned);
        Assert.Equal(session.PlannedEndAt, session.EndedAt);
        var entry = Assert.Single(context.CoinLedgerEntries.Where(c => c.UserId == user.Id));
        Assert.Equal(25, entry.Coins);
    }

    [Fact]
    public async Task Reconciliation_LeavesSessionsWithinTheGraceWindowAlone()
    {
        var (service, clock, context) = Create();
        using var _ = context;
        var user = await AddUserAsync(context);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();
        clock.Advance(TimeSpan.FromMinutes(25 + 5));

        var count = await service.RunAbandonedSessionReconciliationAsync();

        Assert.Equal(0, count);
        Assert.Equal(SessionStatus.Running, session.Status);
    }

    [Fact]
    public async Task Reconciliation_DoesNotKillLegitimatelyLongSessions()
    {
        var (service, clock, context) = Create();
        using var _ = context;
        var user = await AddUserAsync(context);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 180, 5, now: clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();
        clock.Advance(TimeSpan.FromHours(3) + TimeSpan.FromMinutes(5));

        var count = await service.RunAbandonedSessionReconciliationAsync();

        Assert.Equal(0, count);
        Assert.Equal(SessionStatus.Running, session.Status);
    }

    [Fact]
    public async Task Reconciliation_AbandonsLongPausedSessionWithNetTimeUpToThePause()
    {
        var (service, clock, context) = Create();
        using var _ = context;
        var user = await AddUserAsync(context);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 50, 5, now: clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();
        clock.Advance(TimeSpan.FromMinutes(20));
        session.Pause(clock.UtcNow);
        await context.SaveChangesAsync();
        clock.Advance(TimeSpan.FromHours(4));

        var count = await service.RunAbandonedSessionReconciliationAsync();

        Assert.Equal(1, count);
        Assert.Equal(SessionStatus.Abandoned, session.Status);
        Assert.Equal(20 * 60, session.NetDurationSeconds);
        Assert.Equal(20, session.CoinsEarned);
    }

    [Fact]
    public async Task Reconciliation_LeavesRecentlyPausedSessionsAlone()
    {
        var (service, clock, context) = Create();
        using var _ = context;
        var user = await AddUserAsync(context);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 50, 5, now: clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();
        clock.Advance(TimeSpan.FromMinutes(20));
        session.Pause(clock.UtcNow);
        await context.SaveChangesAsync();
        clock.Advance(TimeSpan.FromHours(1));

        var count = await service.RunAbandonedSessionReconciliationAsync();

        Assert.Equal(0, count);
        Assert.Equal(SessionStatus.Paused, session.Status);
    }

    [Fact]
    public async Task Reconciliation_ClearsCoWorkingPresence()
    {
        var (service, clock, context) = Create();
        using var _ = context;
        var user = await AddUserAsync(context);
        var room = StudyRoom.CreatePublicLibrary("Library", "LIB-J", "Test", capacity: 5);
        var member = room.AddMember(user.Id, 1);
        member.SetFocusing(true);
        context.StudyRooms.Add(room);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();
        clock.Advance(TimeSpan.FromHours(2));

        await service.RunAbandonedSessionReconciliationAsync();

        Assert.False(member.IsFocusing);
    }

    [Fact]
    public async Task DailyStreakMaintenance_FreezeCoversMissedDay_SoNextActivityKeepsTheStreak()
    {
        var (service, clock, context) = Create();
        using var _ = context;
        var user = await AddUserAsync(context);
        var streak = UserStreak.CreateDefault(user.Id);
        streak.RecordActivity(clock.UtcNow.AddDays(-2));
        streak.RecordActivity(clock.UtcNow.AddDays(-1));
        context.UserStreaks.Add(streak);
        await context.SaveChangesAsync();

        // Gece yarisi sonrasi: dun (10 Ekim) odaklanilmadi, bugun 11 Ekim 00:30
        clock.Advance(TimeSpan.FromHours(12.5));
        var updated = await service.RunDailyStreakMaintenanceAsync();

        Assert.Equal(1, updated);
        Assert.Equal(0, streak.FreezesAvailable);
        Assert.Equal(2, streak.CurrentStreak);

        var activity = streak.RecordActivity(clock.UtcNow.AddHours(9));

        Assert.False(activity.FreezeUsed);
        Assert.Equal(3, streak.CurrentStreak);
    }
}
