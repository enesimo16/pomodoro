using Focus.Domain.Common;
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public class PixelRoom : BaseEntity<Guid>
{
    private readonly List<RoomItem> _items = new();

    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = "Çalışma Odası";
    public RoomType RoomType { get; private set; } = RoomType.Studio;
    public string? WallPaperId { get; private set; } = "wallpaper_brick_white";
    public string? FloorId { get; private set; } = "floor_parquet_oak";
    public bool IsPublic { get; private set; }
    public string? InviteCode { get; private set; }
    public int MaxVisitors { get; private set; } = 5;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public User OwnerUser { get; private set; } = null!;
    public IReadOnlyCollection<RoomItem> Items => _items.AsReadOnly();

    // EF Core constructor
    private PixelRoom() { }

    public static PixelRoom CreateDefault(Guid ownerUserId, string? roomName = null)
    {
        var room = new PixelRoom
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = roomName ?? "Benim Çalışma Odam",
            RoomType = RoomType.Studio,
            WallPaperId = "wallpaper_brick_white",
            FloorId = "floor_parquet_oak",
            IsPublic = false,
            InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            MaxVisitors = 5,
            CreatedAt = DateTime.UtcNow
        };

        // Başlangıç eşyaları: Çalışma masası, sandalye ve masa lambası
        room._items.Add(new RoomItem(room.Id, "desk_retro_oak", 3, 3, 0));
        room._items.Add(new RoomItem(room.Id, "chair_ergonomic_black", 3, 4, 0));
        room._items.Add(new RoomItem(room.Id, "lamp_desk_brass", 3, 2, 0));

        return room;
    }

    public RoomItem AddItem(string catalogItemId, int gridX, int gridY, int rotation)
    {
        var item = new RoomItem(Id, catalogItemId, gridX, gridY, rotation);
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
        return item;
    }

    public bool RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
            UpdatedAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    public bool MoveItem(Guid itemId, int gridX, int gridY, int rotation)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.Move(gridX, gridY, rotation);
            UpdatedAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    public void UpdateTheme(string? wallPaperId, string? floorId)
    {
        if (!string.IsNullOrWhiteSpace(wallPaperId)) WallPaperId = wallPaperId;
        if (!string.IsNullOrWhiteSpace(floorId)) FloorId = floorId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, bool isPublic, int maxVisitors = 5)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        IsPublic = isPublic;
        if (maxVisitors > 0) MaxVisitors = maxVisitors;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRoomDetails(string name, string? wallPaperId, string? floorId, bool isPublic)
    {
        Name = name;
        WallPaperId = wallPaperId;
        FloorId = floorId;
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;
    }
}
