using FluentAssertions;
using Focus.Application.Features.Room.Queries;
using Focus.Domain.Entities;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Room;

public class RoomQueryHandlerTests
{
    [Fact]
    public async Task GetMyRoom_ShouldReturnExistingRoomWithItems()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var room = PixelRoom.CreateDefault(userId, "Enes Çalışma Odası");
        context.PixelRooms.Add(room);
        await context.SaveChangesAsync();

        var handler = new GetMyRoomQueryHandler(context);
        var result = await handler.Handle(new GetMyRoomQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.OwnerUserId.Should().Be(userId);
        result.Name.Should().Be("Enes Çalışma Odası");
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMyRoom_ShouldCreateDefaultRoom_WhenUserHasNoRoom()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        var handler = new GetMyRoomQueryHandler(context);
        var result = await handler.Handle(new GetMyRoomQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.OwnerUserId.Should().Be(userId);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetRoomById_ShouldReturnPublicRoom_ToAnyVisitor()
    {
        using var context = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var room = PixelRoom.CreateDefault(ownerId, "Açık Ortak Oda");
        room.UpdateDetails("Açık Ortak Oda", isPublic: true);
        context.PixelRooms.Add(room);
        await context.SaveChangesAsync();

        var handler = new GetRoomByIdQueryHandler(context);
        var visitorId = Guid.NewGuid();

        var result = await handler.Handle(new GetRoomByIdQuery(room.Id, visitorId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(room.Id);
    }

    [Fact]
    public async Task GetRoomById_ShouldHidePrivateRoom_FromNonOwners()
    {
        using var context = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var room = PixelRoom.CreateDefault(ownerId, "Gizli Kişisel Oda");
        // isPublic: false
        context.PixelRooms.Add(room);
        await context.SaveChangesAsync();

        var handler = new GetRoomByIdQueryHandler(context);
        var strangerId = Guid.NewGuid();

        var result = await handler.Handle(new GetRoomByIdQuery(room.Id, strangerId), CancellationToken.None);

        result.Should().BeNull();
    }
}
