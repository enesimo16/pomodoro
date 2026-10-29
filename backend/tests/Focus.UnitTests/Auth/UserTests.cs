using FluentAssertions;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Xunit;

namespace Focus.UnitTests.Auth;

public class UserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateGuest_WithBlankName_FallsBackToGeneratedName(string? name)
    {
        var user = User.CreateGuest(name);

        user.DisplayName.Should().StartWith("Misafir#");
    }

    [Fact]
    public void CreateGuest_ShouldInitializeGuestUser_WithDefaultAvatarAndStarterRoom()
    {
        // Act
        var user = User.CreateGuest("TestMisafir");

        // Assert
        user.Should().NotBeNull();
        user.DisplayName.Should().Be("TestMisafir");
        user.IsGuest.Should().BeTrue();
        user.Email.Should().BeNull();
        user.Level.Should().Be(1);
        user.CurrentXp.Should().Be(0);

        // Avatar kontrolu
        user.Avatar.Should().NotBeNull();
        user.Avatar!.UserId.Should().Be(user.Id);
        user.Avatar.SkinTone.Should().Be("tone_1");
        user.Avatar.TopItemCatalogId.Should().Be("tshirt_basic_white");
        user.Avatar.BottomItemCatalogId.Should().Be("jeans_basic_blue");

        // Piksel oda kontrolu
        user.Room.Should().NotBeNull();
        user.Room!.OwnerUserId.Should().Be(user.Id);
        user.Room.RoomType.Should().Be(RoomType.Studio);
        user.Room.Items.Should().HaveCount(3);
        user.Room.Items.Select(i => i.CatalogItemId).Should().Contain(new[]
        {
            "desk_retro_oak",
            "chair_ergonomic_black",
            "lamp_desk_brass"
        });

        // Tercihler kontrolu
        user.Preferences.Should().NotBeNull();
        user.Preferences!.DefaultFocusMinutes.Should().Be(25);
    }

    [Fact]
    public void ClaimWithGoogle_ShouldUpgradeGuestUser_ToRegisteredGoogleUser()
    {
        // Arrange
        var user = User.CreateGuest();
        var googleEmail = "developer@focus.app";
        var googleName = "Enes";

        // Act
        user.ClaimWithGoogle(googleEmail, googleName);

        // Assert
        user.IsGuest.Should().BeFalse();
        user.Email.Should().Be(googleEmail);
        user.DisplayName.Should().Be(googleName);
    }

    [Fact]
    public void AddXp_ShouldIncreaseLevel_WhenThresholdCrossed()
    {
        // Arrange
        var user = User.CreateGuest();

        // Act & Assert
        // Level 2 esigi 150 * 2^1.6 = 455 XP
        user.AddXp(454);
        user.Level.Should().Be(1);

        user.AddXp(1);
        user.Level.Should().Be(2);

        // Level 3 esigi 150 * 3^1.6 = 870 XP
        user.AddXp(415);
        user.Level.Should().Be(3);
    }
}
