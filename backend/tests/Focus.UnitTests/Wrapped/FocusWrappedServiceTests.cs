using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Infrastructure.Persistence;
using Focus.Infrastructure.Services;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Wrapped;

public class FocusWrappedServiceTests
{
    // 2026-10-14 Carsamba 12:00 UTC
    private static readonly DateTime Now = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<(FocusWrappedService Service, ApplicationDbContext Context, User User)> CreateAsync()
    {
        var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("WrappedUser");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return (new FocusWrappedService(context, new TestClock(Now)), context, user);
    }

    private static FocusSession Session(
        User user,
        SessionKind kind,
        DateTime startedAt,
        int plannedMinutes,
        string? themeId = null)
    {
        return FocusSession.Start(user.Id, null, kind, plannedMinutes, plannedMinutes, themeId: themeId, now: startedAt);
    }

    [Fact]
    public async Task Weekly_WithoutAnySessions_ReturnsNoFabricatedHighlights()
    {
        var (service, context, user) = await CreateAsync();
        using var _ = context;

        var wrapped = await service.GetWeeklyWrappedAsync(user.Id);

        Assert.Equal(0, wrapped.TotalFocusMinutes);
        Assert.Equal(0, wrapped.TotalSessionsCompleted);
        Assert.Null(wrapped.CompletionRate);
        Assert.Null(wrapped.TopDayOfWeek);
        Assert.Null(wrapped.PeakTimeOfDay);
        Assert.Null(wrapped.Chronotype);
        Assert.Null(wrapped.ChronotypeName);
        Assert.Null(wrapped.TopTheme);
        Assert.False(string.IsNullOrWhiteSpace(wrapped.SummaryInsight));
    }

    [Fact]
    public async Task Weekly_CountsOnlyFocusSessions()
    {
        var (service, context, user) = await CreateAsync();
        using var _ = context;
        var focus = Session(user, SessionKind.Focus, Now.AddDays(-1).AddHours(-3), 25, "rain");
        focus.Complete(25 * 60, 250, 25, focus.StartedAt.AddMinutes(25));
        var breakSession = Session(user, SessionKind.Break, Now.AddDays(-1), 60);
        breakSession.Complete(60 * 60, 50, 10, breakSession.StartedAt.AddMinutes(60));
        context.FocusSessions.AddRange(focus, breakSession);
        await context.SaveChangesAsync();

        var wrapped = await service.GetWeeklyWrappedAsync(user.Id);

        Assert.Equal(25, wrapped.TotalFocusMinutes);
        Assert.Equal(1, wrapped.TotalSessionsCompleted);
        Assert.Equal(100.0, wrapped.CompletionRate);
        Assert.Equal(250, wrapped.XpEarned);
        Assert.Equal(25, wrapped.CoinsEarned);
        Assert.Equal("rain", wrapped.TopTheme);
    }

    [Fact]
    public async Task Weekly_RunningAndPausedSessionsDoNotLowerTheCompletionRate()
    {
        var (service, context, user) = await CreateAsync();
        using var _ = context;
        var completed = Session(user, SessionKind.Focus, Now.AddDays(-3), 25);
        completed.Complete(25 * 60, 250, 25, completed.StartedAt.AddMinutes(25));
        var abandoned = Session(user, SessionKind.Focus, Now.AddDays(-2), 25);
        abandoned.Abandon(60, endedAt: abandoned.StartedAt.AddMinutes(1));
        var running = Session(user, SessionKind.Focus, Now.AddHours(-1), 25);
        var paused = Session(user, SessionKind.Focus, Now.AddHours(-2), 25);
        paused.Pause(paused.StartedAt.AddMinutes(5));
        context.FocusSessions.AddRange(completed, abandoned, running, paused);
        await context.SaveChangesAsync();

        var wrapped = await service.GetWeeklyWrappedAsync(user.Id);

        Assert.Equal(50.0, wrapped.CompletionRate);
        Assert.Equal(1, wrapped.TotalSessionsCompleted);
    }

    [Fact]
    public async Task Weekly_OnlyActiveSessions_HasNoCompletionRate()
    {
        var (service, context, user) = await CreateAsync();
        using var _ = context;
        context.FocusSessions.Add(Session(user, SessionKind.Focus, Now.AddHours(-1), 25));
        await context.SaveChangesAsync();

        var wrapped = await service.GetWeeklyWrappedAsync(user.Id);

        Assert.Null(wrapped.CompletionRate);
        Assert.Null(wrapped.TopDayOfWeek);
    }

    [Fact]
    public async Task Weekly_DetectsTopDayAndChronotypeFromFocusSessions()
    {
        var (service, context, user) = await CreateAsync();
        using var _ = context;
        // Pazartesi (12 Ekim) iki sabah seansi, Sali bir aksam seansi
        var monday1 = Session(user, SessionKind.Focus, new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc), 50);
        monday1.Complete(50 * 60, 500, 50, monday1.StartedAt.AddMinutes(50));
        var monday2 = Session(user, SessionKind.Focus, new DateTime(2026, 10, 12, 9, 30, 0, DateTimeKind.Utc), 50);
        monday2.Complete(50 * 60, 500, 50, monday2.StartedAt.AddMinutes(50));
        var tuesday = Session(user, SessionKind.Focus, new DateTime(2026, 10, 13, 20, 0, 0, DateTimeKind.Utc), 25);
        tuesday.Complete(25 * 60, 250, 25, tuesday.StartedAt.AddMinutes(25));
        context.FocusSessions.AddRange(monday1, monday2, tuesday);
        await context.SaveChangesAsync();

        var wrapped = await service.GetWeeklyWrappedAsync(user.Id);

        Assert.Equal("Pazartesi", wrapped.TopDayOfWeek);
        Assert.Equal(Chronotype.MorningLark, wrapped.Chronotype);
        Assert.Equal(125, wrapped.TotalFocusMinutes);
        Assert.Null(wrapped.TopTheme);
    }
}
