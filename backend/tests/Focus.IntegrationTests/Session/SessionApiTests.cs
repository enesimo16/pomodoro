using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Session.DTOs;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Session;

public class SessionApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SessionApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartSession_WithCustomDurations_Returns201AndActiveSession()
    {
        var (client, authData) = await _factory.CreateAuthenticatedClientAsync("SayaçTest1");

        var response = await client.PostAsJsonAsync("/api/v1/sessions", new
        {
            kind = "Focus",
            focusMinutes = 48,
            breakMinutes = 17,
            currentRound = 1,
            targetRounds = 4,
            themeId = "cozy_rain"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var session = await response.Content.ReadFromJsonAsync<FocusSessionDto>();
        session.Should().NotBeNull();
        session!.UserId.Should().Be(authData.User.Id);
        session.Kind.Should().Be("Focus");
        session.Status.Should().Be("Running");
        session.FocusMinutes.Should().Be(48);
        session.BreakMinutes.Should().Be(17);
        session.PlannedMinutes.Should().Be(48);
        session.CurrentRound.Should().Be(1);
        session.TargetRounds.Should().Be(4);
    }

    [Fact]
    public async Task GetActiveSession_ReturnsRunningSession()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SayaçTest2");

        // Baslat
        await client.PostAsJsonAsync("/api/v1/sessions", new
        {
            kind = "Focus",
            focusMinutes = 30,
            breakMinutes = 5
        });

        // Aktif seansi sorgula
        var activeResponse = await client.GetAsync("/api/v1/sessions/active");
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var active = await activeResponse.Content.ReadFromJsonAsync<FocusSessionDto>();
        active.Should().NotBeNull();
        active!.Status.Should().Be("Running");
        active.PlannedMinutes.Should().Be(30);
    }

    [Fact]
    public async Task PauseAndResumeSession_UpdatesStatusCorrectly()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SayaçTest3");

        await client.PostAsJsonAsync("/api/v1/sessions", new { focusMinutes = 25, breakMinutes = 5 });

        // Pause
        var pauseRes = await client.PostAsync("/api/v1/sessions/pause", null);
        pauseRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var pausedSession = await pauseRes.Content.ReadFromJsonAsync<FocusSessionDto>();
        pausedSession!.Status.Should().Be("Paused");

        // Resume
        var resumeRes = await client.PostAsync("/api/v1/sessions/resume", null);
        resumeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var resumedSession = await resumeRes.Content.ReadFromJsonAsync<FocusSessionDto>();
        resumedSession!.Status.Should().Be("Running");
    }

    [Fact]
    public async Task ExtendSession_IncreasesPlannedMinutes()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SayaçTest4");

        await client.PostAsJsonAsync("/api/v1/sessions", new { focusMinutes = 25, breakMinutes = 5 });

        var extendRes = await client.PostAsJsonAsync("/api/v1/sessions/extend", new { extraMinutes = 10 });
        extendRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var extended = await extendRes.Content.ReadFromJsonAsync<FocusSessionDto>();
        extended!.PlannedMinutes.Should().Be(35);
        extended.ExtensionCount.Should().Be(1);
    }

    [Fact]
    public async Task CompleteSession_FinishesSessionAndAwardsXpCoins()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SayaçTest5");

        await client.PostAsJsonAsync("/api/v1/sessions", new
        {
            kind = "Focus",
            focusMinutes = 48,
            breakMinutes = 17,
            currentRound = 1,
            targetRounds = 4
        });

        _factory.Clock.Advance(TimeSpan.FromMinutes(48));
        var completeRes = await client.PostAsync("/api/v1/sessions/complete", null);
        completeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await completeRes.Content.ReadFromJsonAsync<SessionCompletionResultDto>();
        result.Should().NotBeNull();
        result!.Session.Status.Should().Be("Completed");
        result.XpEarned.Should().Be(480);
        result.CoinsEarned.Should().Be(48);
        result.NextKind.Should().Be("Break");
        result.NextRound.Should().Be(1);
        result.IsCycleFinished.Should().BeFalse();
    }

    [Fact]
    public async Task AbandonSession_SetsStatusAbandoned()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SayaçTest6");

        await client.PostAsJsonAsync("/api/v1/sessions", new { focusMinutes = 25, breakMinutes = 5 });

        var abandonRes = await client.PostAsync("/api/v1/sessions/abandon", null);
        abandonRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var abandoned = await abandonRes.Content.ReadFromJsonAsync<FocusSessionDto>();
        abandoned!.Status.Should().Be("Abandoned");
    }

    [Fact]
    public async Task CompleteSession_Immediately_AwardsNothing()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SayaçTest7");
        await client.PostAsJsonAsync("/api/v1/sessions", new { focusMinutes = 25, breakMinutes = 5 });

        var completeRes = await client.PostAsync("/api/v1/sessions/complete", null);
        completeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await completeRes.Content.ReadFromJsonAsync<SessionCompletionResultDto>();
        result!.XpEarned.Should().Be(0);
        result.CoinsEarned.Should().Be(0);
        result.TotalCoins.Should().Be(50); // yalnizca misafir hos geldin bonusu
    }

    [Fact]
    public async Task CompleteBreak_WithoutPrecedingFocus_AwardsNothing()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SayaçTest8");
        await client.PostAsJsonAsync("/api/v1/sessions", new { kind = "Break", focusMinutes = 25, breakMinutes = 5 });

        _factory.Clock.Advance(TimeSpan.FromMinutes(5));
        var completeRes = await client.PostAsync("/api/v1/sessions/complete", null);

        var result = await completeRes.Content.ReadFromJsonAsync<SessionCompletionResultDto>();
        result!.XpEarned.Should().Be(0);
        result.CoinsEarned.Should().Be(0);
    }

    [Theory]
    [InlineData("99")]
    [InlineData("Banana")]
    [InlineData("")]
    public async Task StartSession_WithUnknownKind_Returns400(string kind)
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync($"SayaçKind{kind.Length}");

        var response = await client.PostAsJsonAsync("/api/v1/sessions", new { kind, focusMinutes = 25, breakMinutes = 5 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var active = await client.GetAsync("/api/v1/sessions/active");
        active.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(181, 5)]
    [InlineData(25, 0)]
    [InlineData(25, 61)]
    public async Task StartSession_WithOutOfRangeMinutes_Returns400(int focusMinutes, int breakMinutes)
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync($"SayaçRange{focusMinutes}x{breakMinutes}");

        var response = await client.PostAsJsonAsync("/api/v1/sessions", new { kind = "Focus", focusMinutes, breakMinutes });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartSession_Twice_LeavesExactlyOneActiveSession()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SayaçTest9");

        var first = await (await client.PostAsJsonAsync("/api/v1/sessions", new { focusMinutes = 25, breakMinutes = 5 }))
            .Content.ReadFromJsonAsync<FocusSessionDto>();
        var second = await (await client.PostAsJsonAsync("/api/v1/sessions", new { focusMinutes = 30, breakMinutes = 5 }))
            .Content.ReadFromJsonAsync<FocusSessionDto>();

        var active = await (await client.GetAsync("/api/v1/sessions/active")).Content.ReadFromJsonAsync<FocusSessionDto>();
        active!.Id.Should().Be(second!.Id);
        first!.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public async Task ExtendSession_BeyondMaxExtensions_Returns409()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SayaçTest10");
        await client.PostAsJsonAsync("/api/v1/sessions", new { focusMinutes = 25, breakMinutes = 5 });

        for (var i = 0; i < 3; i++)
        {
            (await client.PostAsJsonAsync("/api/v1/sessions/extend", new { extraMinutes = 10 })).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var response = await client.PostAsJsonAsync("/api/v1/sessions/extend", new { extraMinutes = 10 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public async Task ExtendSession_WithInvalidExtraMinutes_Returns400(int extraMinutes)
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync($"SayaçExtend{extraMinutes}");
        await client.PostAsJsonAsync("/api/v1/sessions", new { focusMinutes = 25, breakMinutes = 5 });

        var response = await client.PostAsJsonAsync("/api/v1/sessions/extend", new { extraMinutes });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
