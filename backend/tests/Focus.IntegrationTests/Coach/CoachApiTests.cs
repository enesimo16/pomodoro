using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Coach.DTOs;
using Focus.Application.Features.Session.DTOs;
using Focus.Domain.Enums;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Coach;

public class CoachApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CoachApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Chat_ShouldReturnCoachResponseWithAnomalyAndMemories()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("KocKullanicisi");

        var response = await client.PostAsJsonAsync("/api/v1/coach/chat", new CoachChatRequest("Bugun nasil odaklanabilirim?"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var chatResult = await response.Content.ReadFromJsonAsync<CoachChatResponseDto>();
        chatResult.Should().NotBeNull();
        chatResult!.Reply.Should().NotBeNullOrWhiteSpace();
        chatResult.UsedMemories.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckIn_ShouldSaveMoodAndEnergy()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("CheckInKullanicisi");

        // Seans baslat
        var startRes = await client.PostAsJsonAsync("/api/v1/sessions", new
        {
            kind = "Focus",
            focusMinutes = 25,
            breakMinutes = 5
        });
        var session = await startRes.Content.ReadFromJsonAsync<FocusSessionDto>();

        // Check-in gonder
        var response = await client.PostAsJsonAsync("/api/v1/coach/check-in", new SessionCheckInRequest(
            SessionId: session!.Id,
            Mood: SessionMood.Energized,
            EnergyLevel: 5,
            TargetIntent: "Matematik vize hazirligi"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkIn = await response.Content.ReadFromJsonAsync<SessionCheckInDto>();
        checkIn.Should().NotBeNull();
        checkIn!.Mood.Should().Be(SessionMood.Energized);
        checkIn.EnergyLevel.Should().Be(5);
        checkIn.TargetIntent.Should().Be("Matematik vize hazirligi");
    }

    [Fact]
    public async Task Reflection_ShouldSaveReflectionAndRecordMemories()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("ReflectionKullanicisi");

        // Seans baslat
        var startRes = await client.PostAsJsonAsync("/api/v1/sessions", new
        {
            kind = "Focus",
            focusMinutes = 25,
            breakMinutes = 5
        });
        var session = await startRes.Content.ReadFromJsonAsync<FocusSessionDto>();

        // Reflection gonder
        var response = await client.PostAsJsonAsync("/api/v1/coach/reflection", new SessionReflectionRequest(
            SessionId: session!.Id,
            FocusQuality: 5,
            MoodAfter: SessionMoodAfter.Accomplished,
            DistractionNote: "Yagmur sesi ve sessiz oda ile cok yuksek verim aldim."));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reflection = await response.Content.ReadFromJsonAsync<SessionReflectionDto>();
        reflection.Should().NotBeNull();
        reflection!.FocusQuality.Should().Be(5);
        reflection.MoodAfter.Should().Be(SessionMoodAfter.Accomplished);

        // Bellekleri sorgula
        var memRes = await client.GetAsync("/api/v1/coach/memories");
        memRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var memories = await memRes.Content.ReadFromJsonAsync<List<MemoryInsightDto>>();
        memories.Should().NotBeNull();
        memories!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAnomalyStatus_ShouldReturnStatus()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("AnomaliKullanicisi");

        var response = await client.GetAsync("/api/v1/coach/anomaly-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<CoachAnomalyStatusDto>();
        status.Should().NotBeNull();
        status!.StatusSummary.Should().NotBeNullOrWhiteSpace();
        status.RecommendedAction.Should().NotBeNullOrWhiteSpace();
    }
}
