using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Application.Features.Auth.Queries;
using Focus.IntegrationTests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Focus.IntegrationTests.Auth;

public class GoogleAuthApiTests : IClassFixture<GoogleAuthApiTests.GoogleFactory>
{
    private readonly GoogleFactory _factory;

    public GoogleAuthApiTests(GoogleFactory factory)
    {
        _factory = factory;
    }

    // Id token bicimi: subject|email|emailVerified|ad
    public sealed class GoogleFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGoogleAuthService>();
                services.AddScoped<IGoogleAuthService, FakeGoogleAuthService>();
            });
        }
    }

    private sealed class FakeGoogleAuthService : IGoogleAuthService
    {
        public Task<GoogleUserInfo?> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default)
        {
            var parts = idToken.Split('|');
            if (parts.Length != 4)
            {
                return Task.FromResult<GoogleUserInfo?>(null);
            }

            return Task.FromResult<GoogleUserInfo?>(new GoogleUserInfo(parts[0], parts[1], bool.Parse(parts[2]), parts[3], null));
        }
    }

    private static string Token(string subject, string email, bool verified = true) => $"{subject}|{email}|{verified}|Google Kullanici";

    [Fact]
    public async Task Claim_WithoutAuthentication_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/claim", new { idToken = Token("s1", "a@focus.app") });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Claim_AsGuest_ShouldUpgradeCallerAccount_AndIgnoreBodyGuestId()
    {
        var (victimClient, victim) = await _factory.CreateAuthenticatedClientAsync("Kurban");
        var (client, caller) = await _factory.CreateAuthenticatedClientAsync("Cagiran");

        var response = await client.PostAsJsonAsync("/api/v1/auth/claim", new
        {
            idToken = Token("claim-sub-1", "claim1@focus.app"),
            guestUserIdToClaim = victim.User.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        result.User.Id.Should().Be(caller.User.Id);
        result.User.IsGuest.Should().BeFalse();

        var victimMe = await victimClient.GetAsync("/api/v1/auth/me");
        victimMe.StatusCode.Should().Be(HttpStatusCode.OK);
        var victimProfile = (await victimMe.Content.ReadFromJsonAsync<CurrentUserDto>())!;
        victimProfile.User.IsGuest.Should().BeTrue();
    }

    [Fact]
    public async Task Claim_AsNonGuest_ShouldReturn403()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("Misafir");
        var claimed = await client.PostAsJsonAsync("/api/v1/auth/claim", new { idToken = Token("claim-sub-2", "claim2@focus.app") });
        var claimedData = (await claimed.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", claimedData.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/auth/claim", new { idToken = Token("claim-sub-2", "claim2@focus.app") });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Google_WithoutAuthentication_ShouldNotTouchGuestIdFromBody()
    {
        var (victimClient, victim) = await _factory.CreateAuthenticatedClientAsync("Kurban2");
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/v1/auth/google", new
        {
            idToken = Token("google-sub-1", "google1@focus.app"),
            guestUserIdToClaim = victim.User.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        result.User.Id.Should().NotBe(victim.User.Id);

        var victimMe = await victimClient.GetAsync("/api/v1/auth/me");
        var victimProfile = (await victimMe.Content.ReadFromJsonAsync<CurrentUserDto>())!;
        victimProfile.User.IsGuest.Should().BeTrue();
    }

    [Fact]
    public async Task Google_WithUnverifiedEmail_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/google", new { idToken = Token("google-sub-2", "google2@focus.app", verified: false) });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Google_WithInvalidToken_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/google", new { idToken = "not-a-valid-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
