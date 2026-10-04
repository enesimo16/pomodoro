using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Auth.DTOs;
using Focus.Application.Features.Auth.Queries;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Auth;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GuestLogin_ShouldReturnTokensAndUserProfile()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/guest", new
        {
            displayName = "E2ETestMisafir"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        data.Should().NotBeNull();
        data!.User.DisplayName.Should().Be("E2ETestMisafir");
        data.User.IsGuest.Should().BeTrue();
        data.AccessToken.Should().NotBeNullOrWhiteSpace();
        data.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // Varsayilan avatar ve oda uretilmis olmali
        data.Avatar.Should().NotBeNull();
        data.Room.Should().NotBeNull();
        data.Room!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ShouldReturnCurrentUserProfile()
    {
        var (client, authData) = await _factory.CreateAuthenticatedClientAsync("DogrulanmisKullanici");

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        currentUser.Should().NotBeNull();
        currentUser!.User.Id.Should().Be(authData.User.Id);
        currentUser.User.DisplayName.Should().Be("DogrulanmisKullanici");
        currentUser.TotalCoins.Should().Be(50); // 50 Baslangic Focus Coin hediyesi
    }

    [Fact]
    public async Task GetMe_WithoutToken_ShouldReturn401Unauthorized()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ShouldRotateTokens_AndReturnNewTokens()
    {
        var (client, authData) = await _factory.CreateAuthenticatedClientAsync("RotasyonTesti");

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = authData.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newData = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        newData.Should().NotBeNull();
        newData!.AccessToken.Should().NotBeNullOrWhiteSpace();
        newData.RefreshToken.Should().NotBeNullOrWhiteSpace();
        newData.RefreshToken.Should().NotBe(authData.RefreshToken); // Token rotasyonu
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturn401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = "fake_invalid_token_9999"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldReturn204NoContent()
    {
        var (client, authData) = await _factory.CreateAuthenticatedClientAsync("LogoutTesti");

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new
        {
            refreshToken = authData.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
