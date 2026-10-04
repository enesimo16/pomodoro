using FluentAssertions;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Xunit;

namespace Focus.UnitTests.Room;

public class PixelRoomEntityTests
{
    [Fact]
    public void CreateDefault_ShouldInitializeRoomWithStarterFurniture()
    {
        var ownerId = Guid.NewGuid();

        var room = PixelRoom.CreateDefault(ownerId, "Özel Çalışma Alanı");

        room.Should().NotBeNull();
        room.OwnerUserId.Should().Be(ownerId);
        room.Name.Should().Be("Özel Çalışma Alanı");
        room.RoomType.Should().Be(RoomType.Studio);
        room.WallPaperId.Should().Be("wallpaper_brick_white");
        room.FloorId.Should().Be("floor_parquet_oak");
        room.IsPublic.Should().BeFalse();
        room.InviteCode.Should().NotBeNullOrEmpty();
        room.Items.Should().HaveCount(3);
        room.Items.Select(i => i.CatalogItemId).Should().Contain(new[]
        {
            "desk_retro_oak",
            "chair_ergonomic_black",
            "lamp_desk_brass"
        });
    }

    [Fact]
    public void AddItem_ShouldAppendRoomItem()
    {
        var room = PixelRoom.CreateDefault(Guid.NewGuid());
        var initialCount = room.Items.Count;

        var newItem = room.AddItem("plant_bonsai", 4, 2, 90);

        room.Items.Should().HaveCount(initialCount + 1);
        newItem.CatalogItemId.Should().Be("plant_bonsai");
        newItem.GridX.Should().Be(4);
        newItem.GridY.Should().Be(2);
        newItem.Rotation.Should().Be(90);
        room.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MoveItem_ShouldUpdateCoordinatesAndRotation()
    {
        var room = PixelRoom.CreateDefault(Guid.NewGuid());
        var firstItem = room.Items.First();

        var moved = room.MoveItem(firstItem.Id, 5, 6, 180);

        moved.Should().BeTrue();
        firstItem.GridX.Should().Be(5);
        firstItem.GridY.Should().Be(6);
        firstItem.Rotation.Should().Be(180);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveSpecifiedItem()
    {
        var room = PixelRoom.CreateDefault(Guid.NewGuid());
        var itemToRemove = room.Items.First();
        var initialCount = room.Items.Count;

        var removed = room.RemoveItem(itemToRemove.Id);

        removed.Should().BeTrue();
        room.Items.Should().HaveCount(initialCount - 1);
        room.Items.Should().NotContain(i => i.Id == itemToRemove.Id);
    }

    [Fact]
    public void UpdateTheme_ShouldUpdateWallpaperAndFloor()
    {
        var room = PixelRoom.CreateDefault(Guid.NewGuid());

        room.UpdateTheme("wallpaper_cyber_neon", "floor_tatami_japanese");

        room.WallPaperId.Should().Be("wallpaper_cyber_neon");
        room.FloorId.Should().Be("floor_tatami_japanese");
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateNameAndPrivacy()
    {
        var room = PixelRoom.CreateDefault(Guid.NewGuid());

        room.UpdateDetails("Ortak Kütüphane Odası", isPublic: true, maxVisitors: 10);

        room.Name.Should().Be("Ortak Kütüphane Odası");
        room.IsPublic.Should().BeTrue();
        room.MaxVisitors.Should().Be(10);
    }
}
