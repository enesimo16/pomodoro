using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.Shop;

public class IndexModel : PageModel
{
    private readonly IApplicationDbContext _context;

    public IndexModel(IApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public int TotalItems { get; set; }
    public int FurnitureCount { get; set; }
    public int ClothingCount { get; set; }
    public int TotalPurchasedUnits { get; set; }

    public List<CatalogItemViewModel> Items { get; set; } = new();

    public record CatalogItemViewModel(
        string Id,
        string Name,
        string Description,
        CatalogCategory Category,
        CatalogTier Tier,
        int RequiredLevel,
        int CoinPrice,
        string SpriteUrl,
        bool IsInteractive,
        int GridWidth,
        int GridHeight,
        int PurchaseCount,
        DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        TotalItems = await _context.CatalogItems.CountAsync();
        FurnitureCount = await _context.CatalogItems.CountAsync(i => 
            i.Category == CatalogCategory.Desk || 
            i.Category == CatalogCategory.Chair || 
            i.Category == CatalogCategory.Furniture || 
            i.Category == CatalogCategory.Wallpaper || 
            i.Category == CatalogCategory.Floor);
        ClothingCount = await _context.CatalogItems.CountAsync(i => 
            i.Category == CatalogCategory.Clothing || 
            i.Category == CatalogCategory.Accessory);
        TotalPurchasedUnits = await _context.UserInventoryItems.CountAsync();

        var query = _context.CatalogItems.AsNoTracking();

        if (Filter == "furniture")
        {
            query = query.Where(i => i.Category == CatalogCategory.Desk || 
                                     i.Category == CatalogCategory.Chair || 
                                     i.Category == CatalogCategory.Furniture || 
                                     i.Category == CatalogCategory.Wallpaper || 
                                     i.Category == CatalogCategory.Floor);
        }
        else if (Filter == "clothing")
        {
            query = query.Where(i => i.Category == CatalogCategory.Clothing || 
                                     i.Category == CatalogCategory.Accessory);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var pattern = $"%{Search.Trim()}%";
            query = query.Where(i => EF.Functions.ILike(i.Name, pattern) ||
                                     EF.Functions.ILike(i.Id, pattern));
        }

        var itemsList = await query
            .OrderBy(i => i.Category)
            .ThenBy(i => i.RequiredLevel)
            .ToListAsync();

        var itemIds = itemsList.Select(i => i.Id).ToList();
        var purchaseCounts = await _context.UserInventoryItems
            .Where(inv => itemIds.Contains(inv.CatalogItemId))
            .GroupBy(inv => inv.CatalogItemId)
            .Select(g => new { ItemId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ItemId, x => x.Count);

        Items = itemsList.Select(i => new CatalogItemViewModel(
            i.Id,
            i.Name,
            i.Description,
            i.Category,
            i.Tier,
            i.RequiredLevel,
            i.CoinPrice,
            i.SpriteUrl,
            i.IsInteractive,
            i.GridWidth,
            i.GridHeight,
            purchaseCounts.GetValueOrDefault(i.Id, 0),
            i.CreatedAt)).ToList();
    }

    public async Task<IActionResult> OnPostCreateItemAsync(
        string id,
        string name,
        string description,
        CatalogCategory category,
        CatalogTier tier,
        int requiredLevel,
        int coinPrice,
        string spriteUrl,
        bool isInteractive,
        int gridWidth,
        int gridHeight)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return RedirectToPage();
        }

        var exists = await _context.CatalogItems.AnyAsync(i => i.Id == id.Trim());
        if (!exists)
        {
            var newItem = new CatalogItem(
                id.Trim(),
                name.Trim(),
                string.IsNullOrWhiteSpace(description) ? name.Trim() : description.Trim(),
                category,
                tier,
                Math.Max(1, requiredLevel),
                Math.Max(0, coinPrice),
                string.IsNullOrWhiteSpace(spriteUrl) ? "/assets/items/" + id.Trim() + ".png" : spriteUrl.Trim(),
                isInteractive,
                Math.Max(1, gridWidth),
                Math.Max(1, gridHeight));

            _context.CatalogItems.Add(newItem);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { Search, Filter });
    }
}
