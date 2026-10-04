using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Shop.DTOs;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Shop.Queries;

public record GetCatalogQuery(
    Guid? UserId = null,
    CatalogCategory? Category = null,
    CatalogTier? Tier = null,
    string? Search = null) : IRequest<List<CatalogItemDto>>;

public class GetCatalogQueryHandler : IRequestHandler<GetCatalogQuery, List<CatalogItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCatalogQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CatalogItemDto>> Handle(GetCatalogQuery request, CancellationToken cancellationToken)
    {
        var query = _context.CatalogItems.AsNoTracking().AsQueryable();

        if (request.Category.HasValue)
        {
            query = query.Where(i => i.Category == request.Category.Value);
        }

        if (request.Tier.HasValue)
        {
            query = query.Where(i => i.Tier == request.Tier.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(i => i.Name.Contains(search) || (i.Description != null && i.Description.Contains(search)));
        }

        var items = await query.OrderBy(i => i.Category).ThenBy(i => i.RequiredLevel).ThenBy(i => i.CoinPrice).ToListAsync(cancellationToken);

        HashSet<string> ownedItemIds = new();
        var userLevel = 1;
        var userCoins = 0;

        if (request.UserId.HasValue)
        {
            var userId = request.UserId.Value;
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user != null)
            {
                userLevel = user.Level;
            }

            ownedItemIds = (await _context.UserInventoryItems
                .AsNoTracking()
                .Where(inv => inv.UserId == userId)
                .Select(inv => inv.CatalogItemId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            userCoins = await _context.CoinLedgerEntries
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .SumAsync(c => (int?)c.Coins, cancellationToken) ?? 0;
        }

        return items.Select(item =>
        {
            var isOwned = ownedItemIds.Contains(item.Id);
            var canUnlock = userLevel >= item.RequiredLevel;
            var canAfford = userCoins >= item.CoinPrice;

            return new CatalogItemDto(
                item.Id,
                item.Name,
                item.Description ?? string.Empty,
                item.Category,
                GetCategoryDisplayName(item.Category),
                item.Tier,
                item.RequiredLevel,
                item.CoinPrice,
                item.SpriteUrl,
                item.IsInteractive,
                item.GridWidth,
                item.GridHeight,
                isOwned,
                canAfford,
                canUnlock);
        }).ToList();
    }

    private static string GetCategoryDisplayName(CatalogCategory category) => category switch
    {
        CatalogCategory.Desk => "Çalışma Masası",
        CatalogCategory.Chair => "Koltuk & Sandalye",
        CatalogCategory.Furniture => "Oda Mobilyası & Bitki",
        CatalogCategory.Clothing => "Kıyafet",
        CatalogCategory.Accessory => "Aksesuar & Şapka",
        CatalogCategory.Wallpaper => "Duvar Kağıdı",
        CatalogCategory.Floor => "Zemin Kaplaması",
        _ => category.ToString()
    };
}
