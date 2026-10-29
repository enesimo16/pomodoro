using Focus.Application.Common.Exceptions;
using Focus.Application.Features.Session.Commands;
using Focus.Application.Features.Session.Queries;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Focus.UnitTests.Session;

public class SessionCommandHandlerTests
{
    private static async Task<(SessionTestKit Kit, User User)> CreateKitWithUserAsync(string name = "TimerUser")
    {
        var context = TestDbContextFactory.Create();
        var kit = new SessionTestKit(context);
        var user = User.CreateGuest(name);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return (kit, user);
    }

    [Fact]
    public async Task StartSession_CreatesSession_And_UpdatesUserPreferences()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;

        var command = new StartSessionCommand(
            user.Id,
            SessionKind.Focus,
            FocusMinutes: 48,
            BreakMinutes: 17,
            CurrentRound: 1,
            TargetRounds: 4,
            ThemeId: "rainy_window");

        var result = await kit.StartHandler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Focus", result.Kind);
        Assert.Equal("Running", result.Status);
        Assert.Equal(48, result.FocusMinutes);
        Assert.Equal(17, result.BreakMinutes);
        Assert.Equal(1, result.CurrentRound);
        Assert.Equal(4, result.TargetRounds);
        Assert.Equal(kit.Clock.UtcNow, result.StartedAt);

        var prefs = context.UserPreferences.FirstOrDefault(p => p.UserId == user.Id);
        Assert.NotNull(prefs);
        Assert.Equal(48, prefs.DefaultFocusMinutes);
        Assert.Equal(17, prefs.ShortBreakMinutes);
        Assert.Equal(4, prefs.TargetRounds);
    }

    [Fact]
    public async Task StartSession_AbandonsExistingRunningSession_AndKeepsASingleActiveSession()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var oldSession = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: kit.Clock.UtcNow);
        context.FocusSessions.Add(oldSession);
        await context.SaveChangesAsync();
        kit.Clock.Advance(TimeSpan.FromMinutes(3));

        var result = await kit.StartHandler.Handle(new StartSessionCommand(user.Id, SessionKind.Focus, 48, 17), CancellationToken.None);

        Assert.NotEqual(oldSession.Id, result.Id);
        Assert.Equal(SessionStatus.Abandoned, oldSession.Status);
        Assert.Equal(1, await context.FocusSessions.CountAsync(s => s.UserId == user.Id && (s.Status == SessionStatus.Running || s.Status == SessionStatus.Paused)));
    }

    [Fact]
    public async Task StartSession_WhenOldSessionIsPaused_UsesNetTimeUpToThePause()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var oldSession = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: kit.Clock.UtcNow);
        context.FocusSessions.Add(oldSession);
        await context.SaveChangesAsync();

        // 7 dk calis, 2 saat duraklat
        kit.Clock.Advance(TimeSpan.FromMinutes(7));
        await kit.PauseHandler.Handle(new PauseSessionCommand(user.Id, oldSession.Id), CancellationToken.None);
        kit.Clock.Advance(TimeSpan.FromHours(2));

        await kit.StartHandler.Handle(new StartSessionCommand(user.Id, SessionKind.Focus, 25, 5), CancellationToken.None);

        Assert.Equal(SessionStatus.Abandoned, oldSession.Status);
        Assert.Equal(7 * 60, oldSession.NetDurationSeconds);
    }

    [Fact]
    public async Task PauseAndResume_TogglesSessionState_AndExcludesPausedTimeFromNet()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: kit.Clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        var paused = await kit.PauseHandler.Handle(new PauseSessionCommand(user.Id, session.Id), CancellationToken.None);
        Assert.NotNull(paused);
        Assert.Equal("Paused", paused.Status);

        kit.Clock.Advance(TimeSpan.FromMinutes(10));
        var resumed = await kit.ResumeHandler.Handle(new ResumeSessionCommand(user.Id, session.Id), CancellationToken.None);
        Assert.NotNull(resumed);
        Assert.Equal("Running", resumed.Status);
        Assert.Equal(600, resumed.TotalPausedSeconds);
        Assert.Equal(kit.Clock.UtcNow.AddMinutes(25), resumed.PlannedEndAt);
    }

    [Fact]
    public async Task ExtendSession_AddsExtraMinutes()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: kit.Clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        var extended = await kit.ExtendHandler.Handle(new ExtendSessionCommand(user.Id, 10, session.Id), CancellationToken.None);

        Assert.NotNull(extended);
        Assert.Equal(35, extended.PlannedMinutes);
        Assert.Equal(1, extended.ExtensionCount);
    }

    [Fact]
    public async Task ExtendSession_AllowsOnlyMaxExtensions()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: kit.Clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        for (var i = 0; i < 3; i++)
        {
            await kit.ExtendHandler.Handle(new ExtendSessionCommand(user.Id, 10, session.Id), CancellationToken.None);
        }

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            kit.ExtendHandler.Handle(new ExtendSessionCommand(user.Id, 10, session.Id), CancellationToken.None));

        Assert.Contains("3", ex.Message);
        Assert.Equal(3, session.ExtensionCount);
        Assert.Equal(55, session.PlannedMinutes);
    }

    [Fact]
    public async Task ExtendSession_CapsTotalAtMaxContinuousFocusMinutes()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 85, 5, now: kit.Clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        var extended = await kit.ExtendHandler.Handle(new ExtendSessionCommand(user.Id, 10, session.Id), CancellationToken.None);
        Assert.Equal(90, extended!.PlannedMinutes);

        await Assert.ThrowsAsync<ConflictException>(() =>
            kit.ExtendHandler.Handle(new ExtendSessionCommand(user.Id, 10, session.Id), CancellationToken.None));
    }

    [Fact]
    public async Task ExtendSession_IsRejectedForBreakSessions()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var session = FocusSession.Start(user.Id, null, SessionKind.Break, 25, 5, now: kit.Clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            kit.ExtendHandler.Handle(new ExtendSessionCommand(user.Id, 10, session.Id), CancellationToken.None));
        Assert.Equal(5, session.PlannedMinutes);
    }

    [Fact]
    public async Task AbandonSession_SetsStatusAbandoned()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: kit.Clock.UtcNow);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        var result = await kit.AbandonHandler.Handle(new AbandonSessionCommand(user.Id, session.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Abandoned", result.Status);
    }

    [Fact]
    public async Task GetActiveSession_ReturnsRunningOrPaused()
    {
        var (kit, user) = await CreateKitWithUserAsync();
        using var context = kit.Context;
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 48, 17);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new GetActiveSessionQueryHandler(context);
        var active = await handler.Handle(new GetActiveSessionQuery(user.Id), CancellationToken.None);

        Assert.NotNull(active);
        Assert.Equal(session.Id, active.Id);
        Assert.Equal(48, active.FocusMinutes);
    }

    [Fact]
    public async Task PauseSession_WhenAnotherRequestChangedTheSession_ThrowsConflict()
    {
        var dbName = Guid.NewGuid().ToString();
        using var seed = TestDbContextFactory.CreateShared(dbName);
        var user = User.CreateGuest("RaceUser");
        seed.Users.Add(user);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5);
        seed.FocusSessions.Add(session);
        await seed.SaveChangesAsync();

        using var other = TestDbContextFactory.CreateShared(dbName);
        using var context = TestDbContextFactory.CreateThrowingOnSave(dbName);
        var otherKit = new SessionTestKit(other);
        var kit = new SessionTestKit(context);
        context.BeforeSave = () => otherKit.PauseHandler
            .Handle(new PauseSessionCommand(user.Id, session.Id), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            kit.PauseHandler.Handle(new PauseSessionCommand(user.Id, session.Id), CancellationToken.None));
    }
}
