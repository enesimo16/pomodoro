using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Session.DTOs;
using Focus.Application.Features.Streak.DTOs;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Streak;

public class StreakApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StreakApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStreak_NewUser_ShouldReturnInitialStreakZeroAndOneFreeze()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SeriTestKullanicisi");

        var response = await client.GetAsync("/api/v1/streak");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var streak = await response.Content.ReadFromJsonAsync<UserStreakDto>();
        streak.Should().NotBeNull();
        streak!.CurrentStreak.Should().Be(0);
        streak.FreezesAvailable.Should().Be(1);
        streak.IsActiveToday.Should().BeFalse();
        streak.NextMilestoneDays.Should().Be(3);
    }

    [Fact]
    public async Task CompleteFocusSession_ShouldUpdateStreakToOneAndSetIsActiveToday()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("SeriKazanan");

        // 1. Odak seansı başlat
        var startResponse = await client.PostAsJsonAsync("/api/v1/sessions", new
        {
            kind = "Focus",
            focusMinutes = 25,
            breakMinutes = 5,
            targetRounds = 4
        });
        startResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await startResponse.Content.ReadFromJsonAsync<FocusSessionDto>();

        // 2. Seansı tamamla
        var completeResponse = await client.PostAsync($"/api/v1/sessions/{session!.Id}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Streak kontrolü yap
        var streakResponse = await client.GetAsync("/api/v1/streak");
        streakResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var streak = await streakResponse.Content.ReadFromJsonAsync<UserStreakDto>();
        streak.Should().NotBeNull();
        streak!.CurrentStreak.Should().Be(1);
        streak.IsActiveToday.Should().BeTrue();
    }
}
