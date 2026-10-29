using System.Net;
using FluentAssertions;
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

    [Theory]
    [InlineData("overview")]
    [InlineData("users")]
    [InlineData("economy")]
    [InlineData("health")]
    public async Task AnalyticsEndpoints_AreNotExposedOnPublicApi(string endpoint)
    {
        var anonymous = _factory.CreateClient();
        var (authenticated, _) = await _factory.CreateAuthenticatedClientAsync();

        var anonymousResponse = await anonymous.GetAsync($"/api/v1/admin/analytics/{endpoint}");
        var authenticatedResponse = await authenticated.GetAsync($"/api/v1/admin/analytics/{endpoint}");

        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        authenticatedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
