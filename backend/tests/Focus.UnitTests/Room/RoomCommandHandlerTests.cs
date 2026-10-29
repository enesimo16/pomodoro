using FluentAssertions;
using Focus.Application.Features.Room.Commands;
using Focus.Domain.Entities;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Room;

public class RoomCommandHandlerTests
{
    [Fact]
    public async Task AddRoomItem_ShouldAddOwnedItem()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var room = PixelRoom.CreateDefault(userId);
        context.PixelRooms.Add(room);
        await context.SaveChangesAsync();

        context.UserInventoryItems.Add(new UserInventoryItem(userId, "plant_bonsai"));
        await context.SaveChangesAsync();

        var handler = new AddRoomItemCommandHandler(context);
        var command = new AddRoomItemCommand(
            UserId: userId,
            CatalogItemId: "plant_bonsai",
            GridX: 0,
            GridY: 6,
            Rotation: 90);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.CatalogItemId.Should().Be("plant_bonsai");
        result.GridX.Should().Be(0);
        result.GridY.Should().Be(6);
        result.Rotation.Should().Be(90);

        // Veritabanında kontrol
        var updatedRoom = await context.PixelRooms.FindAsync(room.Id);
        updatedRoom!.Items.Should().Contain(i => i.Id == result.Id);
    }

    [Fact]
    public async Task MoveRoomItem_ShouldUpdatePosition_WhenItemExists()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var room = PixelRoom.CreateDefault(userId);
        context.PixelRooms.Add(room);
        await context.SaveChangesAsync();

        var existingItem = room.Items.First();

        var handler = new MoveRoomItemCommandHandler(context);
        var command = new MoveRoomItemCommand(userId, existingItem.Id, GridX: 6, GridY: 7, Rotation: 180);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result!.GridX.Should().Be(6);
        result.GridY.Should().Be(7);
        result.Rotation.Should().Be(180);
    }

    [Fact]
    public async Task RemoveRoomItem_ShouldReturnTrue_WhenItemRemoved()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var room = PixelRoom.CreateDefault(userId);
        context.PixelRooms.Add(room);
        await context.SaveChangesAsync();

        var itemToRemove = room.Items.First();

        var handler = new RemoveRoomItemCommandHandler(context);
        var result = await handler.Handle(new RemoveRoomItemCommand(userId, itemToRemove.Id), CancellationToken.None);

        result.Should().BeTrue();

        var updatedRoom = await context.PixelRooms.FindAsync(room.Id);
        updatedRoom!.Items.Should().NotContain(i => i.Id == itemToRemove.Id);
    }

    [Fact]
    public async Task UpdateRoomTheme_ShouldUpdateWallpaperAndFloor()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var room = PixelRoom.CreateDefault(userId);
        context.PixelRooms.Add(room);
        await context.SaveChangesAsync();

        var handler = new UpdateRoomThemeCommandHandler(context);
        var command = new UpdateRoomThemeCommand(userId, "wallpaper_brick_white", "floor_parquet_oak");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result!.WallPaperId.Should().Be("wallpaper_brick_white");
        result.FloorId.Should().Be("floor_parquet_oak");
    }

    [Fact]
    public async Task UpdateRoomDetails_ShouldUpdateNameAndPrivacy()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var room = PixelRoom.CreateDefault(userId);
        context.PixelRooms.Add(room);
        await context.SaveChangesAsync();

        var handler = new UpdateRoomDetailsCommandHandler(context);
        var command = new UpdateRoomDetailsCommand(userId, "Gece Kuşları Odası", IsPublic: true, MaxVisitors: 5);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Gece Kuşları Odası");
        result.IsPublic.Should().BeTrue();
        result.MaxVisitors.Should().Be(5);
    }
}
