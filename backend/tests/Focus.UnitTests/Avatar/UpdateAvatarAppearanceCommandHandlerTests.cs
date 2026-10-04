using FluentAssertions;
using Focus.Application.Features.Avatar.Commands;
using Focus.Domain.Entities;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Avatar;

public class UpdateAvatarAppearanceCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUpdateExistingAvatar()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var avatar = UserAvatar.CreateDefault(userId);
        context.UserAvatars.Add(avatar);
        await context.SaveChangesAsync();

        var handler = new UpdateAvatarAppearanceCommandHandler(context);
        var command = new UpdateAvatarAppearanceCommand(
            userId,
            SkinTone: "tone_4",
            HairStyle: "curls",
            HairColor: "black",
            TopItemId: "sweater_green",
            BottomItemId: "jeans_black",
            HatItemId: "beanie_grey",
            GlassesItemId: null,
            ShoesItemId: "sneakers_red");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.SkinTone.Should().Be("tone_4");
        result.HairStyle.Should().Be("curls");
        result.TopItemCatalogId.Should().Be("sweater_green");
        result.HatItemCatalogId.Should().Be("beanie_grey");
    }

    [Fact]
    public async Task Handle_ShouldCreateDefaultAvatarAndApplyUpdates_WhenAvatarDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        var handler = new UpdateAvatarAppearanceCommandHandler(context);
        var command = new UpdateAvatarAppearanceCommand(
            userId,
            SkinTone: "tone_2",
            HairStyle: "short_fade",
            HairColor: "brown",
            TopItemId: null,
            BottomItemId: null,
            HatItemId: null,
            GlassesItemId: null,
            ShoesItemId: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.SkinTone.Should().Be("tone_2");
        result.HairStyle.Should().Be("short_fade");
        result.TopItemCatalogId.Should().Be("tshirt_basic_white"); // Default korunur
    }
}
