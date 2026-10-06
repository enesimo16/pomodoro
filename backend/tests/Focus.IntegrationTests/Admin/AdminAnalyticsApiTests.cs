using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.AdminAnalytics.DTOs;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Admin;

public class AdminAnalyticsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminAnalyticsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOverview_ReturnsOkWithPlatformMetrics()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/analytics/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var overview = await response.Content.ReadFromJsonAsync<PlatformOverviewDto>();
        overview.Should().NotBeNull();
        overview!.TotalUsers.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetUserAnalytics_ReturnsOkWithLevelDistribution()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/analytics/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userStats = await response.Content.ReadFromJsonAsync<UserAnalyticsDto>();
        userStats.Should().NotBeNull();
        userStats!.LevelDistribution.Should().NotBeNull();
        userStats.LevelDistribution.Should().ContainKey("Sv. 1-5");
    }

    [Fact]
    public async Task GetEconomyAnalytics_ReturnsOkWithCirculatingCoins()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/analytics/economy");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var economy = await response.Content.ReadFromJsonAsync<EconomyAnalyticsDto>();
        economy.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSystemHealth_ReturnsOkWithHealthStatus()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/analytics/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var health = await response.Content.ReadFromJsonAsync<SystemHealthDto>();
        health.Should().NotBeNull();
        health!.DatabaseStatus.Should().Be("Healthy");
        health.RedisStatus.Should().Be("Healthy");
    }
}
