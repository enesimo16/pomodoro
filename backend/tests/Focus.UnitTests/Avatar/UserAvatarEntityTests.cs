using FluentAssertions;
using Focus.Domain.Entities;
using Xunit;

namespace Focus.UnitTests.Avatar;

public class UserAvatarEntityTests
{
    [Fact]
    public void CreateDefault_ShouldInitializeDefaultAvatar()
    {
        var userId = Guid.NewGuid();

        var avatar = UserAvatar.CreateDefault(userId);

        avatar.Should().NotBeNull();
        avatar.UserId.Should().Be(userId);
        avatar.SkinTone.Should().Be("tone_1");
        avatar.HairStyle.Should().Be("messy_short");
        avatar.HairColor.Should().Be("dark_brown");
        avatar.TopItemCatalogId.Should().Be("tshirt_basic_white");
        avatar.BottomItemCatalogId.Should().Be("jeans_basic_blue");
        avatar.ShoesItemCatalogId.Should().Be("sneakers_white");
        avatar.HatItemCatalogId.Should().BeNull();
        avatar.GlassesItemCatalogId.Should().BeNull();
    }

    [Fact]
    public void UpdateAppearance_ShouldUpdateProvidedFields()
    {
        var avatar = UserAvatar.CreateDefault(Guid.NewGuid());

        avatar.UpdateAppearance(
            skinTone: "tone_3",
            hairStyle: "ponytail",
            hairColor: "blonde",
            topItemId: "hoodie_black",
            bottomItemId: "cargo_pants",
            hatItemId: "cap_red",
            glassesItemId: "sunglasses_retro",
            shoesItemId: "boots_brown");

        avatar.SkinTone.Should().Be("tone_3");
        avatar.HairStyle.Should().Be("ponytail");
        avatar.HairColor.Should().Be("blonde");
        avatar.TopItemCatalogId.Should().Be("hoodie_black");
        avatar.BottomItemCatalogId.Should().Be("cargo_pants");
        avatar.HatItemCatalogId.Should().Be("cap_red");
        avatar.GlassesItemCatalogId.Should().Be("sunglasses_retro");
        avatar.ShoesItemCatalogId.Should().Be("boots_brown");
        avatar.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateAppearance_ShouldClearAccessories_WhenClearFlagsSet()
    {
        var avatar = UserAvatar.CreateDefault(Guid.NewGuid());
        avatar.UpdateAppearance(hatItemId: "cap_red", glassesItemId: "sunglasses_retro");

        avatar.HatItemCatalogId.Should().Be("cap_red");
        avatar.GlassesItemCatalogId.Should().Be("sunglasses_retro");

        avatar.UpdateAppearance(clearHat: true, clearGlasses: true);

        avatar.HatItemCatalogId.Should().BeNull();
        avatar.GlassesItemCatalogId.Should().BeNull();
    }
}
