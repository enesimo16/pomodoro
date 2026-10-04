using Focus.Domain.Enums;

namespace Focus.Application.Features.Shop.DTOs;

public record CatalogItemDto(
    string Id,
    string Name,
    string Description,
    CatalogCategory Category,
    string CategoryName,
    CatalogTier Tier,
    int RequiredLevel,
    int CoinPrice,
    string SpriteUrl,
    bool IsInteractive,
    int GridWidth,
    int GridHeight,
    bool IsOwned,
    bool CanAfford,
    bool CanUnlock);

public record UserInventoryItemDto(
    Guid Id,
    string CatalogItemId,
    string ItemName,
    string ItemDescription,
    CatalogCategory Category,
    string CategoryName,
    string SpriteUrl,
    bool IsInteractive,
    int GridWidth,
    int GridHeight,
    bool IsEquipped,
    DateTime AcquiredAt);

public record PurchaseItemResultDto(
    bool Success,
    string Message,
    int NewCoinBalance,
    UserInventoryItemDto? InventoryItem);

public record BuyFreezeResultDto(
    bool Success,
    string Message,
    int RemainingCoins,
    int AvailableFreezes);
