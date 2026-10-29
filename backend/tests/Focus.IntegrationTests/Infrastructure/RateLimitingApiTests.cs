using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Infrastructure;

public class RateLimitingApiTests : IClassFixture<RateLimitingApiTests.LimitedFactory>
{
    private readonly LimitedFactory _factory;

    public RateLimitingApiTests(LimitedFactory factory)
    {
        _factory = factory;
    }

    public sealed class LimitedFactory : CustomWebApplicationFactory
    {
        protected override IReadOnlyDictionary<string, string?> TestConfiguration =>
            new Dictionary<string, string?>(base.TestConfiguration)
            {
                ["RateLimiting:Auth:PermitLimit"] = "100000",
                ["RateLimiting:Ai:PermitLimit"] = "2",
                ["RateLimiting:Media:PermitLimit"] = "2"
            };
    }

    [Fact]
    public async Task AiEndpoints_ShouldBeLimitedPerUser()
    {
        var (firstUser, _) = await _factory.CreateAuthenticatedClientAsync("AiKullanici1");
        var (secondUser, _) = await _factory.CreateAuthenticatedClientAsync("AiKullanici2");

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            var response = await firstUser.PostAsJsonAsync("/api/v1/coach/chat", new { });
            statuses.Add(response.StatusCode);
        }

        var otherUser = await secondUser.PostAsJsonAsync("/api/v1/coach/chat", new { });

        statuses.Take(2).Should().NotContain(HttpStatusCode.TooManyRequests);
        statuses[2].Should().Be(HttpStatusCode.TooManyRequests);
        otherUser.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task MediaEndpoints_ShouldReturn429_AfterLimitIsExceeded()
    {
        var client = _factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            var response = await client.GetAsync("/api/v1/media/themes");
            statuses.Add(response.StatusCode);
        }

        statuses.Take(2).Should().NotContain(HttpStatusCode.TooManyRequests);
        statuses[2].Should().Be(HttpStatusCode.TooManyRequests);
    }
}
