using Focus.Domain.Common;

namespace Focus.Domain.Entities;

public class UserAvatar : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public string SkinTone { get; private set; } = "tone_1";
    public string HairStyle { get; private set; } = "messy_short";
    public string HairColor { get; private set; } = "dark_brown";
    public string? TopItemCatalogId { get; private set; } = "tshirt_basic_white";
    public string? BottomItemCatalogId { get; private set; } = "jeans_basic_blue";
    public string? HatItemCatalogId { get; private set; }
    public string? GlassesItemCatalogId { get; private set; }
    public string? ShoesItemCatalogId { get; private set; } = "sneakers_white";
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    // EF Core constructor
    private UserAvatar() { }

    public static UserAvatar CreateDefault(Guid userId)
    {
        return new UserAvatar
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkinTone = "tone_1",
            HairStyle = "messy_short",
            HairColor = "dark_brown",
            TopItemCatalogId = "tshirt_basic_white",
            BottomItemCatalogId = "jeans_basic_blue",
            ShoesItemCatalogId = "sneakers_white",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateAppearance(
        string skinTone,
        string hairStyle,
        string hairColor,
        string? topItemId,
        string? bottomItemId,
        string? hatItemId,
        string? glassesItemId,
        string? shoesItemId)
    {
        SkinTone = skinTone;
        HairStyle = hairStyle;
        HairColor = hairColor;
        TopItemCatalogId = topItemId;
        BottomItemCatalogId = bottomItemId;
        HatItemCatalogId = hatItemId;
        GlassesItemCatalogId = glassesItemId;
        ShoesItemCatalogId = shoesItemId;
        UpdatedAt = DateTime.UtcNow;
    }
}
