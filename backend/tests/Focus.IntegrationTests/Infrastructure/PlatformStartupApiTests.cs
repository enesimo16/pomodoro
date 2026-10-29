using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Auth.Queries;
using Focus.Infrastructure.Persistence;
using Focus.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Focus.IntegrationTests.Infrastructure;

public class PlatformStartupApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlatformStartupApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Startup_WithShortSigningKey_ShouldFailWithClearMessage()
    {
        using var factory = new ShortSigningKeyFactory();

        var act = () => factory.CreateClient();

        act.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("Auth:Jwt:SigningKey");
    }

    [Fact]
    public async Task AuthenticatedRequest_WithHeaderAndConflictingCookie_ShouldUseHeaderIdentity()
    {
        var (_, headerUser) = await _factory.CreateAuthenticatedClientAsync("HeaderKullanici");
        var (_, cookieUser) = await _factory.CreateAuthenticatedClientAsync("CerezKullanici");

        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", headerUser.AccessToken);
        request.Headers.Add("Cookie", $"focus_at={cookieUser.AccessToken}");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        me!.User.Id.Should().Be(headerUser.User.Id);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithValidHeaderAndInvalidCookie_ShouldSucceed()
    {
        var (_, user) = await _factory.CreateAuthenticatedClientAsync("HeaderKullanici2");

        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.AccessToken);
        request.Headers.Add("Cookie", "focus_at=not-a-valid-token");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithCookieOnly_ShouldAuthenticate()
    {
        var (_, user) = await _factory.CreateAuthenticatedClientAsync("CerezKullanicisi");

        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Add("Cookie", $"focus_at={user.AccessToken}");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticatedRequest_ShouldTrackDeviceSessionWithoutTrustingForwardedHeader()
    {
        var (client, user) = await _factory.CreateAuthenticatedClientAsync("TakipKullanicisi");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.77");

        var response = await client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sessions = await db.UserDeviceSessions.Where(s => s.UserId == user.User.Id).ToListAsync();

        sessions.Should().NotBeEmpty();
        sessions.Should().OnlyContain(s => s.IpAddress != "198.51.100.77");
    }

    [Fact]
    public async Task AnonymousRequest_ShouldNotCreateDeviceSession()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AnonymousProbe/1.0");

        await client.PostAsJsonAsync("/api/v1/auth/guest", new { displayName = "AnonimProbe" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var anonymousSessions = await db.UserDeviceSessions.Where(s => s.UserAgent == "AnonymousProbe/1.0").ToListAsync();

        anonymousSessions.Should().BeEmpty();
    }

    private sealed class ShortSigningKeyFactory : CustomWebApplicationFactory
    {
        protected override IReadOnlyDictionary<string, string?> TestConfiguration =>
            new Dictionary<string, string?>(base.TestConfiguration)
            {
                ["Auth:Jwt:SigningKey"] = "too-short"
            };
    }
}
