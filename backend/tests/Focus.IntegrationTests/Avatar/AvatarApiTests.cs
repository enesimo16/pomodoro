using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Auth.DTOs;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Avatar;

public class AvatarApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AvatarApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAvatar_WhenAuthenticated_ShouldReturnAvatar()
    {
        var (client, authData) = await _factory.CreateAuthenticatedClientAsync("AvatarTestKullanici");

        var response = await client.GetAsync("/api/v1/avatar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var avatar = await response.Content.ReadFromJsonAsync<AvatarDto>();
        avatar.Should().NotBeNull();
        avatar!.UserId.Should().Be(authData.User.Id);
        avatar.SkinTone.Should().Be("tone_1");
    }

    [Fact]
    public async Task GetAvatar_WhenUnauthenticated_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/avatar");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateAvatar_ShouldUpdateAppearance_AndReturnUpdatedAvatar()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("AvatarGuncellemeTesti");

        var patchResponse = await client.PatchAsJsonAsync("/api/v1/avatar", new
        {
            skinTone = "tone_3",
            hairStyle = "curls",
            hairColor = "blonde",
            topItemId = "hoodie_black",
            bottomItemId = "jeans_black",
            hatItemId = "cap_red",
            shoesItemId = "sneakers_red"
        });

        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedAvatar = await patchResponse.Content.ReadFromJsonAsync<AvatarDto>();
        updatedAvatar.Should().NotBeNull();
        updatedAvatar!.SkinTone.Should().Be("tone_3");
        updatedAvatar.HairStyle.Should().Be("curls");
        updatedAvatar.HairColor.Should().Be("blonde");
        updatedAvatar.TopItemCatalogId.Should().Be("hoodie_black");
        updatedAvatar.BottomItemCatalogId.Should().Be("jeans_black");
        updatedAvatar.HatItemCatalogId.Should().Be("cap_red");
        updatedAvatar.ShoesItemCatalogId.Should().Be("sneakers_red");
    }

    [Fact]
    public async Task UpdateAvatar_ShouldSupportClearingAccessories()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("AksesuarTemizlemeTesti");

        // Once sapka takalim
        await client.PatchAsJsonAsync("/api/v1/avatar", new
        {
            hatItemId = "beanie_grey"
        });

        // Sapkayi temizleyelim
        var clearResponse = await client.PatchAsJsonAsync("/api/v1/avatar", new
        {
            clearHat = true
        });

        clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var clearedAvatar = await clearResponse.Content.ReadFromJsonAsync<AvatarDto>();
        clearedAvatar!.HatItemCatalogId.Should().BeNull();
    }
}
