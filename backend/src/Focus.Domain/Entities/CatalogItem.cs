using Focus.Domain.Common;
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public class CatalogItem : BaseEntity<string>
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public CatalogCategory Category { get; private set; }
    public CatalogTier Tier { get; private set; }
    public int RequiredLevel { get; private set; }
    public int CoinPrice { get; private set; }
    public string SpriteUrl { get; private set; } = null!;
    public bool IsInteractive { get; private set; }
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private CatalogItem() { }

    public CatalogItem(
        string id,
        string name,
        string description,
        CatalogCategory category,
        CatalogTier tier,
        int requiredLevel,
        int coinPrice,
        string spriteUrl,
        bool isInteractive = false,
        int gridWidth = 1,
        int gridHeight = 1)
    {
        Id = id;
        Name = name;
        Description = description;
        Category = category;
        Tier = tier;
        RequiredLevel = Math.Max(1, requiredLevel);
        CoinPrice = Math.Max(0, coinPrice);
        SpriteUrl = spriteUrl;
        IsInteractive = isInteractive;
        GridWidth = Math.Max(1, gridWidth);
        GridHeight = Math.Max(1, gridHeight);
        CreatedAt = DateTime.UtcNow;
    }
}
