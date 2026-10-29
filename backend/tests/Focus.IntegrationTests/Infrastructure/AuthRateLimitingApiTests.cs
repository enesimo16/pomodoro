using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Infrastructure;

public class AuthRateLimitingApiTests : IClassFixture<AuthRateLimitingApiTests.LimitedFactory>
{
    private readonly LimitedFactory _factory;

    public AuthRateLimitingApiTests(LimitedFactory factory)
    {
        _factory = factory;
    }

    public sealed class LimitedFactory : CustomWebApplicationFactory
    {
        protected override IReadOnlyDictionary<string, string?> TestConfiguration =>
            new Dictionary<string, string?>(base.TestConfiguration)
            {
                ["RateLimiting:Auth:PermitLimit"] = "2"
            };
    }

    [Fact]
    public async Task AuthEndpoints_ShouldReturn429_AfterLimitIsExceeded()
    {
        var client = _factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "a" });
        var second = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "b" });
        var third = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "c" });

        first.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        third.Headers.Contains("Retry-After").Should().BeTrue();
    }
}
