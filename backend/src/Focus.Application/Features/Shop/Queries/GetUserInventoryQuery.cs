using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Shop.DTOs;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Shop.Queries;

public record GetUserInventoryQuery(Guid UserId, CatalogCategory? Category = null) : IRequest<List<UserInventoryItemDto>>;

public class GetUserInventoryQueryHandler : IRequestHandler<GetUserInventoryQuery, List<UserInventoryItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserInventoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserInventoryItemDto>> Handle(GetUserInventoryQuery request, CancellationToken cancellationToken)
    {
        var query = _context.UserInventoryItems
            .Include(i => i.CatalogItem)
            .AsNoTracking()
            .Where(i => i.UserId == request.UserId);

        if (request.Category.HasValue)
        {
            query = query.Where(i => i.CatalogItem.Category == request.Category.Value);
        }

        var inventory = await query
            .OrderByDescending(i => i.AcquiredAt)
            .ToListAsync(cancellationToken);

        return inventory.Select(i => new UserInventoryItemDto(
            i.Id,
            i.CatalogItemId,
            i.CatalogItem?.Name ?? i.CatalogItemId,
            i.CatalogItem?.Description ?? string.Empty,
            i.CatalogItem?.Category ?? CatalogCategory.Furniture,
            GetCategoryDisplayName(i.CatalogItem?.Category ?? CatalogCategory.Furniture),
            i.CatalogItem?.SpriteUrl ?? string.Empty,
            i.CatalogItem?.IsInteractive ?? false,
            i.CatalogItem?.GridWidth ?? 1,
            i.CatalogItem?.GridHeight ?? 1,
            i.IsEquipped,
            i.AcquiredAt
        )).ToList();
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
