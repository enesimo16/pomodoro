using Focus.Domain.Common;

namespace Focus.Domain.Entities;

public class RoomItem : BaseEntity<Guid>
{
    public Guid RoomId { get; private set; }
    public string CatalogItemId { get; private set; } = null!;
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public int Rotation { get; private set; }
    public DateTime PlacedAt { get; private set; }

    // Navigation
    public PixelRoom Room { get; private set; } = null!;

    // EF Core constructor
    private RoomItem() { }

    public RoomItem(Guid roomId, string catalogItemId, int gridX, int gridY, int rotation)
    {
        Id = Guid.NewGuid();
        RoomId = roomId;
        CatalogItemId = catalogItemId;
        GridX = gridX;
        GridY = gridY;
        Rotation = rotation;
        PlacedAt = DateTime.UtcNow;
    }

    public void Move(int gridX, int gridY, int rotation)
    {
        GridX = gridX;
        GridY = gridY;
        Rotation = rotation;
    }
}
