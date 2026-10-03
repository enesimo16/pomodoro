using Focus.Domain.Entities;

namespace Focus.Application.Features.Auth.DTOs;

public record RoomItemDto(
    Guid Id,
    string CatalogItemId,
    int GridX,
    int GridY,
    int Rotation);

public record PixelRoomDto(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string RoomType,
    string? WallPaperId,
    string? FloorId,
    bool IsPublic,
    string? InviteCode,
    int MaxVisitors,
    List<RoomItemDto> Items)
{
    public static PixelRoomDto FromEntity(PixelRoom room)
    {
        return new PixelRoomDto(
            room.Id,
            room.OwnerUserId,
            room.Name,
            room.RoomType.ToString(),
            room.WallPaperId,
            room.FloorId,
            room.IsPublic,
            room.InviteCode,
            room.MaxVisitors,
            room.Items.Select(i => new RoomItemDto(i.Id, i.CatalogItemId, i.GridX, i.GridY, i.Rotation)).ToList());
    }
}
