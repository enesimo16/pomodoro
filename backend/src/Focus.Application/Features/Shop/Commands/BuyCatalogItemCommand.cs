using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Shop.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Shop.Commands;

public record BuyCatalogItemCommand(Guid UserId, string CatalogItemId) : IRequest<PurchaseItemResultDto>;

public class BuyCatalogItemCommandHandler : IRequestHandler<BuyCatalogItemCommand, PurchaseItemResultDto>
{
    private readonly IApplicationDbContext _context;

    public BuyCatalogItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseItemResultDto> Handle(BuyCatalogItemCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
        {
            return new PurchaseItemResultDto(false, "Kullanıcı bulunamadı.", 0, null);
        }

        var item = await _context.CatalogItems.FirstOrDefaultAsync(i => i.Id == request.CatalogItemId, cancellationToken);
        if (item == null)
        {
            return new PurchaseItemResultDto(false, "Eşya katalogda bulunamadı.", 0, null);
        }

        var currentCoins = await _context.CoinLedgerEntries
            .Where(c => c.UserId == request.UserId)
            .SumAsync(c => (int?)c.Coins, cancellationToken) ?? 0;

        if (user.Level < item.RequiredLevel)
        {
            return new PurchaseItemResultDto(
                false,
                $"Bu eşyayı açmak için Seviye {item.RequiredLevel} olmalısınız (Mevcut Seviyeniz: {user.Level}).",
                currentCoins,
                null);
        }

        var alreadyOwned = await _context.UserInventoryItems
            .AnyAsync(i => i.UserId == request.UserId && i.CatalogItemId == request.CatalogItemId, cancellationToken);

        if (alreadyOwned)
        {
            return new PurchaseItemResultDto(false, "Bu eşyaya zaten sahipsiniz.", currentCoins, null);
        }

        if (currentCoins < item.CoinPrice)
        {
            return new PurchaseItemResultDto(
                false,
                $"Yetersiz bakiye! Bu eşya için {item.CoinPrice} Focus Coin gerekiyor. Mevcut bakiyeniz: {currentCoins}.",
                currentCoins,
                null);
        }

        if (item.CoinPrice > 0)
        {
            var coinEntry = new CoinLedgerEntry(request.UserId, CoinTransactionReason.ItemPurchase, -item.CoinPrice);
            _context.CoinLedgerEntries.Add(coinEntry);
        }

        var inventoryItem = new UserInventoryItem(request.UserId, item.Id);
        _context.UserInventoryItems.Add(inventoryItem);

        await _context.SaveChangesAsync(cancellationToken);

        var newBalance = currentCoins - item.CoinPrice;
        var dto = new UserInventoryItemDto(
            inventoryItem.Id,
            item.Id,
            item.Name,
            item.Description ?? string.Empty,
            item.Category,
            GetCategoryDisplayName(item.Category),
            item.SpriteUrl,
            item.IsInteractive,
            item.GridWidth,
            item.GridHeight,
            inventoryItem.IsEquipped,
            inventoryItem.AcquiredAt);

        return new PurchaseItemResultDto(true, $"'{item.Name}' başarıyla satın alındı ve envanterinize eklendi!", newBalance, dto);
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
