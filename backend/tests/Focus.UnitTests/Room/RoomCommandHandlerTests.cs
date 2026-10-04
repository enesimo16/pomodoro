using FluentAssertions;
using Focus.Application.Features.Room.Commands;
using Focus.Domain.Entities;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Room;

public class RoomCommandHandlerTests
{
    [Fact]
    public async Task AddRoomItem_ShouldAddItem_AndNormalizeCoordinatesAndRotation()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var room = PixelRoom.CreateDefault(userId);
        context.PixelRooms.Add(room);
        await context.SaveChangesAsync();

        var handler = new AddRoomItemCommandHandler(context);
        var command = new AddRoomItemCommand(
            UserId: userId,
            CatalogItemId: "plant_fiddle_leaf",
            GridX: -5, // Negatif koordinat 0'a çekilmeli
            GridY: 3,
            Rotation: 450); // 450 % 360 = 90 dereceye normalize edilmeli

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.CatalogItemId.Should().Be("plant_fiddle_leaf");
        result.GridX.Should().Be(0);
        result.GridY.Should().Be(3);
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
        var command = new MoveRoomItemCommand(userId, existingItem.Id, GridX: 7, GridY: 8, Rotation: 180);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result!.GridX.Should().Be(7);
        result.GridY.Should().Be(8);
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
        var command = new UpdateRoomThemeCommand(userId, "wallpaper_retro_sunset", "floor_marble_white");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result!.WallPaperId.Should().Be("wallpaper_retro_sunset");
        result.FloorId.Should().Be("floor_marble_white");
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
        var command = new UpdateRoomDetailsCommand(userId, "Gece Kuşları Odası", IsPublic: true, MaxVisitors: 8);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Gece Kuşları Odası");
        result.IsPublic.Should().BeTrue();
        result.MaxVisitors.Should().Be(8);
    }
}
