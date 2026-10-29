using FluentAssertions;
using FluentValidation;
using Focus.Application.Features.Atmosphere.Commands;
using Focus.Application.Features.Atmosphere.DTOs;
using Focus.Application.Features.Avatar.Commands;
using Focus.Application.Features.Room.Commands;
using Focus.Application.Features.Room.Queries;
using Focus.Domain.Entities;
using Focus.Infrastructure.Persistence;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Room;

public class RoomOwnershipAndPlacementTests
{
    private static async Task<(ApplicationDbContext Context, Guid UserId, PixelRoom Room)> CreateRoomAsync()
    {
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var room = PixelRoom.CreateDefault(userId);
        context.PixelRooms.Add(room);
        await context.SaveChangesAsync();
        return (context, userId, room);
    }

    private static async Task GrantAsync(ApplicationDbContext context, Guid userId, string itemId)
    {
        context.UserInventoryItems.Add(new UserInventoryItem(userId, itemId));
        await context.SaveChangesAsync();
    }

    private static AddRoomItemCommand Add(Guid userId, string itemId, int x, int y, int rotation = 0)
        => new(userId, itemId, x, y, rotation);

    [Fact]
    public async Task AddItem_NotOwned_ShouldBeRejected()
    {
        var (context, userId, _) = await CreateRoomAsync();
        using var db = context;

        var act = () => new AddRoomItemCommandHandler(context).Handle(Add(userId, "fireplace_cozy", 6, 6), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddItem_UnknownOrWrongCategory_ShouldBeRejected()
    {
        var (context, userId, _) = await CreateRoomAsync();
        using var db = context;
        var handler = new AddRoomItemCommandHandler(context);

        await handler.Invoking(h => h.Handle(Add(userId, "does_not_exist", 6, 6), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
        await handler.Invoking(h => h.Handle(Add(userId, "wallpaper_brick_white", 6, 6), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddItem_OutOfBoundsOccupiedOrBadRotation_ShouldBeRejected()
    {
        var (context, userId, _) = await CreateRoomAsync();
        using var db = context;
        await GrantAsync(context, userId, "plant_bonsai");
        await GrantAsync(context, userId, "plant_monstera");
        var handler = new AddRoomItemCommandHandler(context);

        await handler.Invoking(h => h.Handle(Add(userId, "plant_bonsai", -1, 0), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
        await handler.Invoking(h => h.Handle(Add(userId, "plant_bonsai", PixelRoom.GridWidth, 0), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
        await handler.Invoking(h => h.Handle(Add(userId, "plant_bonsai", 6, 6, 45), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
        // Baslangic masasi (3,3) ve (4,3) karolarini kaplar
        await handler.Invoking(h => h.Handle(Add(userId, "plant_bonsai", 4, 3), CancellationToken.None)).Should().ThrowAsync<ValidationException>();

        var ok = await handler.Handle(Add(userId, "plant_bonsai", 8, 8), CancellationToken.None);
        ok.GridX.Should().Be(8);
        await handler.Invoking(h => h.Handle(Add(userId, "plant_bonsai", 0, 8), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddItem_ShouldRejectWhenRoomIsFull()
    {
        var (context, userId, room) = await CreateRoomAsync();
        using var db = context;
        await GrantAsync(context, userId, "plant_bonsai");
        while (room.Items.Count < PixelRoom.MaxItems)
        {
            context.RoomItems.Add(room.AddItem("filler", 0, 0, 0));
        }

        await context.SaveChangesAsync();

        var act = () => new AddRoomItemCommandHandler(context).Handle(Add(userId, "plant_bonsai", 8, 8), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task MoveItem_ShouldRespectBoundsOverlapAndRotation()
    {
        var (context, userId, room) = await CreateRoomAsync();
        using var db = context;
        var desk = room.Items.First(i => i.CatalogItemId == "desk_retro_oak");
        var handler = new MoveRoomItemCommandHandler(context);

        await handler.Invoking(h => h.Handle(new MoveRoomItemCommand(userId, desk.Id, 9, 9, 0), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
        await handler.Invoking(h => h.Handle(new MoveRoomItemCommand(userId, desk.Id, 3, 3, 33), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
        // Dondurulunce 2x1 masa 1x2 olur ve sandalyenin (3,4) karesine biner
        await handler.Invoking(h => h.Handle(new MoveRoomItemCommand(userId, desk.Id, 3, 3, 90), CancellationToken.None)).Should().ThrowAsync<ValidationException>();

        var moved = await handler.Handle(new MoveRoomItemCommand(userId, desk.Id, 6, 6, 0), CancellationToken.None);
        moved!.GridX.Should().Be(6);
    }

    [Fact]
    public async Task Theme_ShouldRequireOwnershipAndMatchingCategory()
    {
        var (context, userId, _) = await CreateRoomAsync();
        using var db = context;
        var handler = new UpdateRoomThemeCommandHandler(context);

        await handler.Invoking(h => h.Handle(new UpdateRoomThemeCommand(userId, "wallpaper_slate_dark", null), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
        await handler.Invoking(h => h.Handle(new UpdateRoomThemeCommand(userId, "floor_parquet_oak", null), CancellationToken.None)).Should().ThrowAsync<ValidationException>();

        await GrantAsync(context, userId, "wallpaper_slate_dark");
        var result = await handler.Handle(new UpdateRoomThemeCommand(userId, "wallpaper_slate_dark", null), CancellationToken.None);
        result!.WallPaperId.Should().Be("wallpaper_slate_dark");
    }

    [Fact]
    public async Task Avatar_ShouldRejectUnownedAndWrongSlotItems()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var handler = new UpdateAvatarAppearanceCommandHandler(context);

        UpdateAvatarAppearanceCommand Command(string? top = null, string? hat = null) =>
            new(userId, null, null, null, top, null, hat, null, null);

        await handler.Invoking(h => h.Handle(Command(top: "jacket_leather_vintage"), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
        await handler.Invoking(h => h.Handle(Command(top: "desk_retro_oak"), CancellationToken.None)).Should().ThrowAsync<ValidationException>();
        await handler.Invoking(h => h.Handle(Command(hat: "hoodie_gray"), CancellationToken.None)).Should().ThrowAsync<ValidationException>();

        var result = await handler.Handle(Command(top: "hoodie_gray"), CancellationToken.None);
        result.TopItemCatalogId.Should().Be("hoodie_gray");
    }

    [Fact]
    public async Task ApplyAtmosphere_ShouldRejectUnownedWallpaper()
    {
        var (context, userId, _) = await CreateRoomAsync();
        using var db = context;
        var handler = new ApplyAtmosphereCommandHandler(context);
        var request = new ApplyAtmosphereRequestDto { WallPaperId = "wallpaper_wood_cozy" };

        var act = () => handler.Handle(new ApplyAtmosphereCommand(userId, request), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RoomDetails_ShouldEnforceNameAndVisitorLimits()
    {
        var (context, userId, _) = await CreateRoomAsync();
        using var db = context;
        var handler = new UpdateRoomDetailsCommandHandler(context);

        await handler.Invoking(h => h.Handle(new UpdateRoomDetailsCommand(userId, "Oda", true, 6), CancellationToken.None)).Should().ThrowAsync<ValidationException>();

        var validator = new UpdateRoomDetailsCommandValidator();
        validator.Validate(new UpdateRoomDetailsCommand(userId, new string('a', 101), true, 3)).IsValid.Should().BeFalse();
        validator.Validate(new UpdateRoomDetailsCommand(userId, "  ", true, 3)).IsValid.Should().BeFalse();
        validator.Validate(new UpdateRoomDetailsCommand(userId, "Oda", true, 0)).IsValid.Should().BeFalse();
        validator.Validate(new UpdateRoomDetailsCommand(userId, "Oda", true, 3)).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoomById_ShouldHideInviteCodeFromNonOwners()
    {
        var (context, userId, room) = await CreateRoomAsync();
        using var db = context;
        await new UpdateRoomDetailsCommandHandler(context).Handle(new UpdateRoomDetailsCommand(userId, "Acik", true, 3), CancellationToken.None);
        var handler = new GetRoomByIdQueryHandler(context);

        var asOwner = await handler.Handle(new GetRoomByIdQuery(room.Id, userId), CancellationToken.None);
        var asVisitor = await handler.Handle(new GetRoomByIdQuery(room.Id, Guid.NewGuid()), CancellationToken.None);

        asOwner!.InviteCode.Should().NotBeNullOrEmpty();
        asVisitor!.InviteCode.Should().BeNull();
    }
}
