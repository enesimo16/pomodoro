using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Auth.DTOs;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Room;

public class RoomApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoomApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyRoom_ShouldReturnUserRoomWithDefaultFurniture()
    {
        var (client, authData) = await _factory.CreateAuthenticatedClientAsync("OdaSahibi");

        var response = await client.GetAsync("/api/v1/room");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var room = await response.Content.ReadFromJsonAsync<PixelRoomDto>();
        room.Should().NotBeNull();
        room!.OwnerUserId.Should().Be(authData.User.Id);
        room.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddItemToRoom_ShouldPlaceFurnitureAndReturnCreated()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("MobilyaEkleyen");

        var addResponse = await client.PostAsJsonAsync("/api/v1/room/items", new
        {
            catalogItemId = "plant_bonsai",
            gridX = 4,
            gridY = 2,
            rotation = 90
        });

        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var item = await addResponse.Content.ReadFromJsonAsync<RoomItemDto>();
        item.Should().NotBeNull();
        item!.CatalogItemId.Should().Be("plant_bonsai");
        item.GridX.Should().Be(4);
        item.GridY.Should().Be(2);
        item.Rotation.Should().Be(90);

        // Odayi cektigimizde mobilya listede gorunmeli
        var roomResponse = await client.GetAsync("/api/v1/room");
        var room = await roomResponse.Content.ReadFromJsonAsync<PixelRoomDto>();
        room!.Items.Should().Contain(i => i.Id == item.Id);
    }

    [Fact]
    public async Task MoveRoomItem_ShouldUpdateCoordinatesAndRotation()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("MobilyaTasiyan");

        var roomResponse = await client.GetAsync("/api/v1/room");
        var room = await roomResponse.Content.ReadFromJsonAsync<PixelRoomDto>();
        var firstItem = room!.Items.First();

        var moveResponse = await client.PutAsJsonAsync($"/api/v1/room/items/{firstItem.Id}", new
        {
            gridX = 7,
            gridY = 8,
            rotation = 180
        });

        moveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedItem = await moveResponse.Content.ReadFromJsonAsync<RoomItemDto>();
        updatedItem!.GridX.Should().Be(7);
        updatedItem.GridY.Should().Be(8);
        updatedItem.Rotation.Should().Be(180);
    }

    [Fact]
    public async Task RemoveRoomItem_ShouldDeleteFurniture()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("MobilyaSilen");

        var roomResponse = await client.GetAsync("/api/v1/room");
        var room = await roomResponse.Content.ReadFromJsonAsync<PixelRoomDto>();
        var itemToRemove = room!.Items.First();

        var deleteResponse = await client.DeleteAsync($"/api/v1/room/items/{itemToRemove.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Silinen esya artik odada olmamali
        var roomAfterDelete = await (await client.GetAsync("/api/v1/room")).Content.ReadFromJsonAsync<PixelRoomDto>();
        roomAfterDelete!.Items.Should().NotContain(i => i.Id == itemToRemove.Id);
    }

    [Fact]
    public async Task UpdateRoomTheme_ShouldUpdateWallpaperAndFloor()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("TemaGuncelleyen");

        var response = await client.PatchAsJsonAsync("/api/v1/room/theme", new
        {
            wallpaperCatalogId = "wallpaper_cyber_neon",
            floorCatalogId = "floor_tatami_japanese"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var room = await response.Content.ReadFromJsonAsync<PixelRoomDto>();
        room!.WallPaperId.Should().Be("wallpaper_cyber_neon");
        room.FloorId.Should().Be("floor_tatami_japanese");
    }

    [Fact]
    public async Task UpdateRoomDetails_ShouldUpdateNameAndVisibility()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("DetayGuncelleyen");

        var response = await client.PutAsJsonAsync("/api/v1/room/details", new
        {
            name = "Gezginler Kahvesi",
            isPublic = true,
            maxVisitors = 8
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var room = await response.Content.ReadFromJsonAsync<PixelRoomDto>();
        room!.Name.Should().Be("Gezginler Kahvesi");
        room.IsPublic.Should().BeTrue();
        room.MaxVisitors.Should().Be(8);
    }

    [Fact]
    public async Task GetRoomById_PublicRoom_ShouldBeAccessibleByOtherUsers()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync("OdaSahibi2");

        // Odayi herkese acik yapalim
        var patchResponse = await ownerClient.PutAsJsonAsync("/api/v1/room/details", new
        {
            name = "Herkese Açık Ortak Alan",
            isPublic = true,
            maxVisitors = 5
        });
        var publicRoom = await patchResponse.Content.ReadFromJsonAsync<PixelRoomDto>();

        // Baska bir kullanici odayi goruntulesin
        var (strangerClient, _) = await _factory.CreateAuthenticatedClientAsync("YabanciKullanici");
        var viewResponse = await strangerClient.GetAsync($"/api/v1/room/{publicRoom!.Id}");

        viewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var viewedRoom = await viewResponse.Content.ReadFromJsonAsync<PixelRoomDto>();
        viewedRoom!.Name.Should().Be("Herkese Açık Ortak Alan");
    }

    [Fact]
    public async Task GetRoomById_PrivateRoom_ShouldReturn404ToOtherUsers()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync("OdaSahibi3");

        // Oda varsayilan olarak private (isPublic = false)
        var room = await (await ownerClient.GetAsync("/api/v1/room")).Content.ReadFromJsonAsync<PixelRoomDto>();

        // Baska bir kullanici odaya erismeye calissin
        var (strangerClient, _) = await _factory.CreateAuthenticatedClientAsync("GizliOdayaGirmeyeCalisan");
        var viewResponse = await strangerClient.GetAsync($"/api/v1/room/{room!.Id}");

        viewResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
