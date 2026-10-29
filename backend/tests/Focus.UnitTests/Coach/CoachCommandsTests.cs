using FluentAssertions;
using Focus.Application.Common.Exceptions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.Commands;
using Focus.Application.Features.Coach.Queries;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Persistence;
using Focus.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Focus.UnitTests.Coach;

public class CoachCommandsTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Mock<IVectorMemoryService> NoEmbeddingVectorService()
    {
        var vector = new Mock<IVectorMemoryService>();
        vector.Setup(v => v.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((float[]?)null);
        vector.Setup(v => v.AddPrivateMemoryAsync(It.IsAny<Guid>(), It.IsAny<MemoryCategory>(), It.IsAny<string>(), It.IsAny<float[]?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return vector;
    }

    private static async Task<(ApplicationDbContext Context, User User, FocusSession Session)> SeedAsync(bool complete)
    {
        var context = CreateContext();
        var user = User.CreateGuest("Kerem");
        context.Users.Add(user);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 45, 10);
        if (complete)
        {
            session.Complete(45 * 60, 50, 10);
        }

        context.FocusSessions.Add(session);
        await context.SaveChangesAsync();
        return (context, user, session);
    }

    private static SubmitSessionReflectionCommandHandler CreateReflectionHandler(ApplicationDbContext context) =>
        new(context, NoEmbeddingVectorService().Object, new AnonymizationService());

    [Fact]
    public async Task SubmitReflection_CompletedHighQualitySession_AddsOneGlobalMemoryLinkedToSession()
    {
        var (context, user, session) = await SeedAsync(complete: true);
        var handler = CreateReflectionHandler(context);
        var command = new SubmitSessionReflectionCommand(user.Id, session.Id, 5, SessionMoodAfter.Accomplished, "sessiz kutuphane iyi geldi");

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        var globals = await context.AgentMemories.Where(m => m.UserId == null).ToListAsync();
        globals.Should().ContainSingle().Which.SourceSessionId.Should().Be(session.Id);
    }

    [Fact]
    public async Task SubmitReflection_RunningSession_DoesNotContributeToGlobalPool()
    {
        var (context, user, session) = await SeedAsync(complete: false);

        await CreateReflectionHandler(context).Handle(
            new SubmitSessionReflectionCommand(user.Id, session.Id, 5, SessionMoodAfter.Accomplished, "not"), CancellationToken.None);

        (await context.AgentMemories.AnyAsync(m => m.UserId == null)).Should().BeFalse();
    }

    [Fact]
    public async Task SubmitReflection_WhenAgentDisabled_DoesNotContributeToGlobalPool()
    {
        var (context, user, session) = await SeedAsync(complete: true);
        var prefs = await context.UserPreferences.FirstAsync(p => p.UserId == user.Id);
        prefs.UpdatePreferences(25, 5, 15, false, true, null, agentEnabled: false);
        await context.SaveChangesAsync();

        await CreateReflectionHandler(context).Handle(
            new SubmitSessionReflectionCommand(user.Id, session.Id, 5, SessionMoodAfter.Accomplished, "not"), CancellationToken.None);

        (await context.AgentMemories.AnyAsync(m => m.UserId == null)).Should().BeFalse();
    }

    [Fact]
    public async Task Validators_RejectOverlongText()
    {
        var reflection = await new SubmitSessionReflectionCommandValidator().ValidateAsync(
            new SubmitSessionReflectionCommand(Guid.NewGuid(), Guid.NewGuid(), 3, SessionMoodAfter.Accomplished, new string('x', 351)));
        var checkIn = await new SubmitSessionCheckInCommandValidator().ValidateAsync(
            new SubmitSessionCheckInCommand(Guid.NewGuid(), Guid.NewGuid(), SessionMood.Stressed, 3, new string('x', 251)));
        var chat = await new SendCoachChatCommandValidator().ValidateAsync(new SendCoachChatCommand(Guid.NewGuid(), new string('x', 1001)));

        reflection.IsValid.Should().BeFalse();
        checkIn.IsValid.Should().BeFalse();
        chat.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AnomalyStatus_WhenLastSessionIsOlderThanTwoWeeks_StillDetectsAbsenceReturn()
    {
        var context = CreateContext();
        var user = User.CreateGuest("Uzun");
        context.Users.Add(user);
        var old = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5);
        typeof(FocusSession).GetProperty(nameof(FocusSession.StartedAt))!.SetValue(old, DateTime.UtcNow.AddDays(-30));
        context.FocusSessions.Add(old);
        await context.SaveChangesAsync();

        var handler = new GetCoachAnomalyStatusQueryHandler(context, new FocusCoachEngine());
        var status = await handler.Handle(new GetCoachAnomalyStatusQuery(user.Id), CancellationToken.None);

        status.AnomalyType.Should().Be(AnomalyType.AbsenceReturn);
        status.DaysSinceLastSession.Should().BeGreaterThanOrEqualTo(29);
    }

    [Fact]
    public async Task AiUsageService_EnforcesDailyBudgetPerUserAndDay()
    {
        var context = CreateContext();
        var user = User.CreateGuest("Kota");
        var other = User.CreateGuest("Diger");
        context.Users.AddRange(user, other);
        await context.SaveChangesAsync();
        var clock = new FakeClock(new DateTimeOffset(2026, 10, 29, 10, 0, 0, TimeSpan.Zero));
        var service = new AiUsageService(context, Options.Create(new AiSettings { DailyTokenBudgetPerUser = 1000 }), clock);

        (await service.TryConsumeAsync(user.Id, 600)).Should().BeTrue();
        (await service.TryConsumeAsync(user.Id, 500)).Should().BeFalse();
        (await service.TryConsumeAsync(user.Id, 400)).Should().BeTrue();
        (await service.TryConsumeAsync(other.Id, 900)).Should().BeTrue();

        clock.Now = clock.Now.AddDays(1);
        (await service.TryConsumeAsync(user.Id, 900)).Should().BeTrue();

        var act = () => service.ConsumeOrThrowAsync(user.Id, 500);
        await act.Should().ThrowAsync<AiBudgetExceededException>();
    }

    private sealed class FakeClock : TimeProvider
    {
        public FakeClock(DateTimeOffset now)
        {
            Now = now;
        }

        public DateTimeOffset Now { get; set; }

        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void FatigueScore_CountsNightWorkInUserLocalTime()
    {
        var engine = new FocusCoachEngine();
        var session = FocusSession.Start(Guid.NewGuid(), null, SessionKind.Focus, 60, 5);
        typeof(FocusSession).GetProperty(nameof(FocusSession.StartedAt))!.SetValue(session, new DateTime(2026, 10, 29, 22, 30, 0, DateTimeKind.Utc));

        var utcScore = engine.CalculateDailyFatigueScore([session], TimeZoneInfo.Utc);
        var istanbulScore = engine.CalculateDailyFatigueScore([session], TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"));

        istanbulScore.Should().BeGreaterThan(utcScore);
    }
}
