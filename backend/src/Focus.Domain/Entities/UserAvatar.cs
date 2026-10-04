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
        string? skinTone = null,
        string? hairStyle = null,
        string? hairColor = null,
        string? topItemId = null,
        string? bottomItemId = null,
        string? hatItemId = null,
        string? glassesItemId = null,
        string? shoesItemId = null,
        bool clearHat = false,
        bool clearGlasses = false)
    {
        if (!string.IsNullOrWhiteSpace(skinTone)) SkinTone = skinTone;
        if (!string.IsNullOrWhiteSpace(hairStyle)) HairStyle = hairStyle;
        if (!string.IsNullOrWhiteSpace(hairColor)) HairColor = hairColor;
        if (!string.IsNullOrWhiteSpace(topItemId)) TopItemCatalogId = topItemId;
        if (!string.IsNullOrWhiteSpace(bottomItemId)) BottomItemCatalogId = bottomItemId;
        if (!string.IsNullOrWhiteSpace(shoesItemId)) ShoesItemCatalogId = shoesItemId;

        if (clearHat) HatItemCatalogId = null;
        else if (hatItemId != null) HatItemCatalogId = hatItemId;

        if (clearGlasses) GlassesItemCatalogId = null;
        else if (glassesItemId != null) GlassesItemCatalogId = glassesItemId;

        UpdatedAt = DateTime.UtcNow;
    }
}
