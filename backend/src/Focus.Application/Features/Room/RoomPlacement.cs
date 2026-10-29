using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Room;

public static class RoomPlacement
{
    // Gecerli donusler disinda deger kabul edilmez
    public static int RequireRotation(int rotation)
    {
        return PixelRoom.IsValidRotation(rotation)
            ? rotation
            : throw new ValidationException("Dönüş açısı 0, 90, 180 veya 270 olmalıdır.");
    }

    // Parca izi odanin izgarasi icinde kalmali ve diger parcalarla cakismamali
    public static async Task EnsureFitsAsync(
        IApplicationDbContext context,
        PixelRoom room,
        CatalogItem item,
        int gridX,
        int gridY,
        int rotation,
        Guid? ignoreItemId,
        CancellationToken cancellationToken)
    {
        var (width, height) = PixelRoom.Footprint(item.GridWidth, item.GridHeight, rotation);
        if (!PixelRoom.IsWithinGrid(gridX, gridY, width, height))
        {
            throw new ValidationException($"Eşya oda ızgarasının ({PixelRoom.GridWidth}x{PixelRoom.GridHeight}) dışında kalıyor.");
        }

        var others = room.Items.Where(i => i.Id != ignoreItemId).ToList();
        var otherIds = others.Select(i => i.CatalogItemId).Distinct().ToList();
        var sizes = await context.CatalogItems.AsNoTracking()
            .Where(c => otherIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        foreach (var other in others)
        {
            var (ow, oh) = sizes.TryGetValue(other.CatalogItemId, out var catalog)
                ? PixelRoom.Footprint(catalog.GridWidth, catalog.GridHeight, other.Rotation)
                : (1, 1);

            if (PixelRoom.Overlaps(gridX, gridY, width, height, other.GridX, other.GridY, ow, oh))
            {
                throw new ValidationException("Bu karo dolu.");
            }
        }
    }
}
