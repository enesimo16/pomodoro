using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Session.DTOs;
using Focus.Application.Features.Stats.DTOs;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Stats;

public class StatsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StatsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStats_ShouldReturnUserStatisticsWith7DaysAndLevel()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("IstatistikSever");

        // 1. Ilk istatistikleri al
        var initialResponse = await client.GetAsync("/api/v1/stats");
        initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialStats = await initialResponse.Content.ReadFromJsonAsync<UserStatsDto>();
        initialStats.Should().NotBeNull();
        initialStats!.TotalCompletedSessions.Should().Be(0);
        initialStats.TotalFocusMinutes.Should().Be(0);
        initialStats.CurrentLevel.Should().Be(1);
        initialStats.Last7Days.Should().HaveCount(7);

        // 2. Bir odaklanma seansi baslat ve tamamla
        var startResponse = await client.PostAsJsonAsync("/api/v1/sessions", new
        {
            kind = "Focus",
            focusMinutes = 30,
            breakMinutes = 5,
            targetRounds = 4
        });
        startResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await startResponse.Content.ReadFromJsonAsync<FocusSessionDto>();

        var completeResponse = await client.PostAsync($"/api/v1/sessions/{session!.Id}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Istatistikleri tekrar al ve dogrula
        var updatedResponse = await client.GetAsync("/api/v1/stats");
        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedStats = await updatedResponse.Content.ReadFromJsonAsync<UserStatsDto>();
        updatedStats.Should().NotBeNull();
        updatedStats!.TotalCompletedSessions.Should().Be(1);
        updatedStats.TodaySessionCount.Should().Be(1);
        updatedStats.CurrentStreak.Should().Be(1);
        updatedStats.Last7Days.Should().HaveCount(7);
    }
}
