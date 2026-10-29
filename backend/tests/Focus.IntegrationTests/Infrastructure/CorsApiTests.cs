using FluentAssertions;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Infrastructure;

public class CorsApiTests : IClassFixture<CustomWebApplicationFactory>, IClassFixture<ProductionWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _developmentFactory;
    private readonly ProductionWebApplicationFactory _productionFactory;

    public CorsApiTests(CustomWebApplicationFactory developmentFactory, ProductionWebApplicationFactory productionFactory)
    {
        _developmentFactory = developmentFactory;
        _productionFactory = productionFactory;
    }

    private static async Task<string?> GetAllowedOriginAsync(HttpClient client, string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/guest");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values)
            ? values.FirstOrDefault()
            : null;
    }

    [Fact]
    public async Task Preflight_InDevelopment_ShouldAllowArbitraryLocalhostOrigin()
    {
        var client = _developmentFactory.CreateClient();

        var allowedOrigin = await GetAllowedOriginAsync(client, "http://localhost:4321");

        allowedOrigin.Should().Be("http://localhost:4321");
    }

    [Fact]
    public async Task Preflight_InProduction_ShouldRejectUnlistedLocalhostOrigin()
    {
        var client = _productionFactory.CreateClient();

        var allowedOrigin = await GetAllowedOriginAsync(client, "http://localhost:4321");

        allowedOrigin.Should().BeNull();
    }

    [Fact]
    public async Task Preflight_InProduction_ShouldAllowExactlyConfiguredOrigin()
    {
        var client = _productionFactory.CreateClient();

        var allowedOrigin = await GetAllowedOriginAsync(client, "http://localhost:3000");

        allowedOrigin.Should().Be("http://localhost:3000");
    }

    [Fact]
    public async Task Preflight_InProduction_ShouldRejectForeignOrigin()
    {
        var client = _productionFactory.CreateClient();

        var allowedOrigin = await GetAllowedOriginAsync(client, "https://evil.example.com");

        allowedOrigin.Should().BeNull();
    }
}
