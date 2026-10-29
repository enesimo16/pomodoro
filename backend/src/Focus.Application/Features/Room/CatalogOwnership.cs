using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Room;

public static class CatalogOwnership
{
    public static readonly IReadOnlySet<CatalogCategory> FurnitureCategories =
        new HashSet<CatalogCategory> { CatalogCategory.Desk, CatalogCategory.Chair, CatalogCategory.Furniture };

    public static readonly IReadOnlySet<CatalogCategory> ClothingCategories =
        new HashSet<CatalogCategory> { CatalogCategory.Clothing };

    public static readonly IReadOnlySet<CatalogCategory> AccessoryCategories =
        new HashSet<CatalogCategory> { CatalogCategory.Accessory };

    public static readonly IReadOnlySet<CatalogCategory> WallpaperCategories =
        new HashSet<CatalogCategory> { CatalogCategory.Wallpaper };

    public static readonly IReadOnlySet<CatalogCategory> FloorCategories =
        new HashSet<CatalogCategory> { CatalogCategory.Floor };

    // Katalogda bulunmali, yuva kategorisine uymali ve kullaniciya ait olmali; ucretsiz baslangic esyalari envanter kaydi gerektirmez
    public static async Task<CatalogItem> RequireOwnedAsync(
        IApplicationDbContext context,
        Guid userId,
        string? catalogItemId,
        IReadOnlySet<CatalogCategory> allowedCategories,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(catalogItemId))
        {
            throw new ValidationException("Eşya kimliği zorunludur.");
        }

        var item = await context.CatalogItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == catalogItemId, cancellationToken);

        if (item == null || !allowedCategories.Contains(item.Category))
        {
            throw new ValidationException("Geçersiz eşya seçimi.");
        }

        if (IsStarter(item)) return item;

        var owned = await context.UserInventoryItems.AsNoTracking()
            .AnyAsync(i => i.UserId == userId && i.CatalogItemId == item.Id, cancellationToken);

        return owned ? item : throw new ValidationException("Bu eşyaya sahip değilsiniz.");
    }

    private static bool IsStarter(CatalogItem item) => item.Tier == CatalogTier.Free && item.CoinPrice == 0;
}
