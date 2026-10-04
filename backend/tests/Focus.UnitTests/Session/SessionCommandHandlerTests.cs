using Focus.Application.Features.Session.Commands;
using Focus.Application.Features.Session.Queries;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Session;

public class SessionCommandHandlerTests
{
    [Fact]
    public async Task StartSession_CreatesSession_And_UpdatesUserPreferences()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("TimerUser");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new StartSessionCommandHandler(context);
        var command = new StartSessionCommand(
            user.Id,
            SessionKind.Focus,
            FocusMinutes: 48,
            BreakMinutes: 17,
            CurrentRound: 1,
            TargetRounds: 4,
            ThemeId: "rainy_window");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Focus", result.Kind);
        Assert.Equal("Running", result.Status);
        Assert.Equal(48, result.FocusMinutes);
        Assert.Equal(17, result.BreakMinutes);
        Assert.Equal(1, result.CurrentRound);
        Assert.Equal(4, result.TargetRounds);

        var prefs = context.UserPreferences.FirstOrDefault(p => p.UserId == user.Id);
        Assert.NotNull(prefs);
        Assert.Equal(48, prefs.DefaultFocusMinutes);
        Assert.Equal(17, prefs.ShortBreakMinutes);
        Assert.Equal(4, prefs.TargetRounds);
    }

    [Fact]
    public async Task StartSession_AbandonsExistingRunningSession()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("TimerUser");
        context.Users.Add(user);
        var oldSession = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5);
        context.FocusSessions.Add(oldSession);
        await context.SaveChangesAsync();

        var handler = new StartSessionCommandHandler(context);
        var command = new StartSessionCommand(user.Id, SessionKind.Focus, 48, 17);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEqual(oldSession.Id, result.Id);
        Assert.Equal(SessionStatus.Abandoned, oldSession.Status);
    }

    [Fact]
    public async Task PauseAndResume_TogglesSessionState()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("TimerUser");
        context.Users.Add(user);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        // Pause
        var pauseHandler = new PauseSessionCommandHandler(context);
        var paused = await pauseHandler.Handle(new PauseSessionCommand(user.Id, session.Id), CancellationToken.None);
        Assert.NotNull(paused);
        Assert.Equal("Paused", paused.Status);

        // Resume
        var resumeHandler = new ResumeSessionCommandHandler(context);
        var resumed = await resumeHandler.Handle(new ResumeSessionCommand(user.Id, session.Id), CancellationToken.None);
        Assert.NotNull(resumed);
        Assert.Equal("Running", resumed.Status);
    }

    [Fact]
    public async Task ExtendSession_AddsExtraMinutes()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("TimerUser");
        context.Users.Add(user);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new ExtendSessionCommandHandler(context);
        var extended = await handler.Handle(new ExtendSessionCommand(user.Id, 10, session.Id), CancellationToken.None);

        Assert.NotNull(extended);
        Assert.Equal(35, extended.PlannedMinutes);
        Assert.Equal(1, extended.ExtensionCount);
    }

    [Fact]
    public async Task CompleteSession_AwardsXpAndCoins_AndProgression()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("TimerUser");
        context.Users.Add(user);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 48, 17, 1, 4);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new CompleteSessionCommandHandler(context);
        var result = await handler.Handle(new CompleteSessionCommand(user.Id, session.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.XpEarned >= 0);
        Assert.True(result.CoinsEarned >= 0);
        Assert.Equal("Break", result.NextKind);
        Assert.Equal(1, result.NextRound);
        Assert.False(result.IsCycleFinished);

        // Verify coin ledger
        var entries = context.CoinLedgerEntries.Where(c => c.UserId == user.Id).ToList();
        Assert.True(entries.Count >= 1);
        Assert.Equal(CoinTransactionReason.SessionReward, entries.Last().Reason);
    }

    [Fact]
    public async Task AbandonSession_SetsStatusAbandoned()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("TimerUser");
        context.Users.Add(user);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new AbandonSessionCommandHandler(context);
        var result = await handler.Handle(new AbandonSessionCommand(user.Id, session.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Abandoned", result.Status);
    }

    [Fact]
    public async Task GetActiveSession_ReturnsRunningOrPaused()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("TimerUser");
        context.Users.Add(user);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 48, 17);
        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new GetActiveSessionQueryHandler(context);
        var active = await handler.Handle(new GetActiveSessionQuery(user.Id), CancellationToken.None);

        Assert.NotNull(active);
        Assert.Equal(session.Id, active.Id);
        Assert.Equal(48, active.FocusMinutes);
    }
}
