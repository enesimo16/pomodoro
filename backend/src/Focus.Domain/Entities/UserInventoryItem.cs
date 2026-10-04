using Focus.Domain.Common;

namespace Focus.Domain.Entities;

public class UserInventoryItem : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public string CatalogItemId { get; private set; } = null!;
    public DateTime AcquiredAt { get; private set; }
    public bool IsEquipped { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;
    public CatalogItem CatalogItem { get; private set; } = null!;

    // EF Core constructor
    private UserInventoryItem() { }

    public UserInventoryItem(Guid userId, string catalogItemId, bool isEquipped = false)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CatalogItemId = catalogItemId;
        AcquiredAt = DateTime.UtcNow;
        IsEquipped = isEquipped;
    }

    public void SetEquipped(bool equipped)
    {
        IsEquipped = equipped;
    }
}
