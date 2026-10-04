using Focus.Application.Features.Shop.Commands;
using Focus.Application.Features.Shop.Queries;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Shop;

public class ShopCommandHandlerTests
{
    [Fact]
    public async Task BuyCatalogItem_WhenUserHasSufficientCoinsAndLevel_ShouldSucceedAndDeductCoins()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("Shopper");
        user.AddXp(2000); // Level up
        context.Users.Add(user);

        // Add 500 coins
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 500));
        await context.SaveChangesAsync();

        var handler = new BuyCatalogItemCommandHandler(context);
        // desk_minimal_white costs 80 coins and requires level 3
        var result = await handler.Handle(new BuyCatalogItemCommand(user.Id, "desk_minimal_white"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(420, result.NewCoinBalance);
        Assert.NotNull(result.InventoryItem);
        Assert.Equal("desk_minimal_white", result.InventoryItem.CatalogItemId);
    }

    [Fact]
    public async Task BuyCatalogItem_WhenUserHasInsufficientCoins_ShouldFail()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("PoorShopper");
        user.AddXp(5000);
        context.Users.Add(user);
        // Only 10 coins
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 10));
        await context.SaveChangesAsync();

        var handler = new BuyCatalogItemCommandHandler(context);
        var result = await handler.Handle(new BuyCatalogItemCommand(user.Id, "desk_minimal_white"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Yetersiz bakiye", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuyCatalogItem_WhenUserLevelTooLow_ShouldFail()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("LowLevelShopper"); // Level 1
        context.Users.Add(user);
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 1000));
        await context.SaveChangesAsync();

        var handler = new BuyCatalogItemCommandHandler(context);
        // desk_cyber_neon requires level 8
        var result = await handler.Handle(new BuyCatalogItemCommand(user.Id, "desk_cyber_neon"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Seviye 8", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuyCatalogItem_WhenAlreadyOwned_ShouldFail()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("DuplicateShopper");
        user.AddXp(3000);
        context.Users.Add(user);
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 1000));
        context.UserInventoryItems.Add(new UserInventoryItem(user.Id, "desk_minimal_white"));
        await context.SaveChangesAsync();

        var handler = new BuyCatalogItemCommandHandler(context);
        var result = await handler.Handle(new BuyCatalogItemCommand(user.Id, "desk_minimal_white"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("zaten sahipsiniz", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuyStreakFreeze_WhenUserHasCoinsAndUnderLimit_ShouldSucceed()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("FreezeBuyer");
        context.Users.Add(user);
        var streak = UserStreak.CreateDefault(user.Id);
        // Default has 1 freeze, limit is 2
        context.UserStreaks.Add(streak);
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 250));
        await context.SaveChangesAsync();

        var handler = new BuyStreakFreezeCommandHandler(context);
        var result = await handler.Handle(new BuyStreakFreezeCommand(user.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(150, result.RemainingCoins);
        Assert.Equal(2, result.AvailableFreezes);
    }

    [Fact]
    public async Task BuyStreakFreeze_WhenMaxFreezesReached_ShouldFail()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("MaxFreezeBuyer");
        context.Users.Add(user);
        var streak = UserStreak.CreateDefault(user.Id);
        streak.AddFreeze(1); // Now at 2
        context.UserStreaks.Add(streak);
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 500));
        await context.SaveChangesAsync();

        var handler = new BuyStreakFreezeCommandHandler(context);
        var result = await handler.Handle(new BuyStreakFreezeCommand(user.Id), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("maksimum", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCatalog_WithCategoryFilter_ShouldReturnFilteredItems()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("CatalogViewer");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetCatalogQueryHandler(context);
        var result = await handler.Handle(new GetCatalogQuery(user.Id, Category: CatalogCategory.Desk), CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.All(result, item => Assert.Equal(CatalogCategory.Desk, item.Category));
    }

    [Fact]
    public async Task GetUserInventory_ShouldReturnUserItems()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("InventoryOwner");
        context.Users.Add(user);
        context.UserInventoryItems.Add(new UserInventoryItem(user.Id, "desk_retro_oak"));
        context.UserInventoryItems.Add(new UserInventoryItem(user.Id, "chair_ergonomic_black"));
        await context.SaveChangesAsync();

        var handler = new GetUserInventoryQueryHandler(context);
        var result = await handler.Handle(new GetUserInventoryQuery(user.Id), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.CatalogItemId == "desk_retro_oak");
        Assert.Contains(result, i => i.CatalogItemId == "chair_ergonomic_black");
    }
}
