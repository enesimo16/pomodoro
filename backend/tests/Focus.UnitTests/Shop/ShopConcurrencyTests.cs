using Focus.Application.Common.Exceptions;
using Focus.Application.Features.Shop.Commands;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Infrastructure.Persistence;
using Focus.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Focus.UnitTests.Shop;

public class ShopConcurrencyTests
{
    [Fact]
    public async Task BuyCatalogItem_WhenTwoPurchasesRaceOnTheSameBalance_SecondOneIsRejectedAsConflict()
    {
        var dbName = Guid.NewGuid().ToString();
        using var seed = TestDbContextFactory.CreateShared(dbName);
        var user = User.CreateGuest("DoubleSpender");
        user.AddXp(10_000);
        seed.Users.Add(user);
        seed.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 300));
        await seed.SaveChangesAsync();

        using var first = TestDbContextFactory.CreateShared(dbName);
        using var second = TestDbContextFactory.CreateThrowingOnSave(dbName);

        // Ikinci istek bakiyeyi (300) okuduktan sonra, kayittan hemen once birinci istek commit eder
        second.BeforeSave = () => new BuyCatalogItemCommandHandler(first)
            .Handle(new BuyCatalogItemCommand(user.Id, "desk_cyber_neon"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new BuyCatalogItemCommandHandler(second)
                .Handle(new BuyCatalogItemCommand(user.Id, "chair_gaming_rgb"), CancellationToken.None));

    }

    [Fact]
    public async Task BuyCatalogItem_WhenUniqueIndexRejectsDuplicate_ReturnsAlreadyOwned()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid userId = Guid.Empty;

        // Kayit aninda baska bir istek ayni esyayi coktan kaydetmis olur
        using var context = TestDbContextFactory.CreateThrowingOnSave(dbName, () =>
        {
            using var winner = TestDbContextFactory.CreateShared(dbName);
            winner.UserInventoryItems.Add(new UserInventoryItem(userId, "desk_retro_oak"));
            winner.SaveChanges();
            return new DbUpdateException("duplicate key value violates unique constraint");
        });

        var user = User.CreateGuest("RacingBuyer");
        userId = user.Id;
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ThrowOnSave = true;

        var result = await new BuyCatalogItemCommandHandler(context)
            .Handle(new BuyCatalogItemCommand(user.Id, "desk_retro_oak"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("zaten sahipsiniz", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuyCatalogItem_WhenSaveFailsForAnotherReason_Rethrows()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = TestDbContextFactory.CreateThrowingOnSave(dbName, () => new DbUpdateException("connection lost"));
        var user = User.CreateGuest("UnluckyBuyer");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ThrowOnSave = true;

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new BuyCatalogItemCommandHandler(context)
                .Handle(new BuyCatalogItemCommand(user.Id, "desk_retro_oak"), CancellationToken.None));
    }

    [Fact]
    public async Task BuyCatalogItem_WhenItemIsProOnlyAndUserIsNotPro_FailsWithoutCharging()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("FreeUser");
        user.AddXp(10_000);
        context.Users.Add(user);
        context.CatalogItems.Add(CreateProItem());
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 500));
        await context.SaveChangesAsync();

        var result = await new BuyCatalogItemCommandHandler(context)
            .Handle(new BuyCatalogItemCommand(user.Id, "pro_gold_trophy"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Pro", result.Message);
        Assert.Empty(context.UserInventoryItems.Where(i => i.UserId == user.Id));
        Assert.Equal(500, await context.CoinLedgerEntries.Where(c => c.UserId == user.Id).SumAsync(c => c.Coins));
    }

    [Fact]
    public async Task BuyCatalogItem_WhenItemIsProOnlyAndUserIsPro_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("ProUser");
        user.SetPro(true);
        context.Users.Add(user);
        context.CatalogItems.Add(CreateProItem());
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 500));
        await context.SaveChangesAsync();

        var result = await new BuyCatalogItemCommandHandler(context)
            .Handle(new BuyCatalogItemCommand(user.Id, "pro_gold_trophy"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(400, result.NewCoinBalance);
    }

    [Fact]
    public async Task BuyStreakFreeze_WhenTwoPurchasesRace_SecondOneIsRejectedAsConflict()
    {
        var dbName = Guid.NewGuid().ToString();
        using var seed = TestDbContextFactory.CreateShared(dbName);
        var user = User.CreateGuest("FreezeRacer");
        seed.Users.Add(user);
        seed.UserStreaks.Add(UserStreak.CreateDefault(user.Id));
        seed.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 150));
        await seed.SaveChangesAsync();

        using var first = TestDbContextFactory.CreateShared(dbName);
        using var second = TestDbContextFactory.CreateThrowingOnSave(dbName);

        second.BeforeSave = () => new BuyStreakFreezeCommandHandler(first)
            .Handle(new BuyStreakFreezeCommand(user.Id), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new BuyStreakFreezeCommandHandler(second).Handle(new BuyStreakFreezeCommand(user.Id), CancellationToken.None));

    }

    [Fact]
    public async Task BuyStreakFreeze_WhenAlreadyAtCap_DoesNotCharge()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("CappedBuyer");
        context.Users.Add(user);
        var streak = UserStreak.CreateDefault(user.Id);
        streak.AddFreeze(1);
        context.UserStreaks.Add(streak);
        context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 500));
        await context.SaveChangesAsync();

        var result = await new BuyStreakFreezeCommandHandler(context)
            .Handle(new BuyStreakFreezeCommand(user.Id), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(500, await context.CoinLedgerEntries.Where(c => c.UserId == user.Id).SumAsync(c => c.Coins));
        Assert.DoesNotContain(context.CoinLedgerEntries, c => c.Reason == CoinTransactionReason.StreakFreezePurchase);
    }

    private static CatalogItem CreateProItem() => new(
        "pro_gold_trophy",
        "Pro Kupa",
        "Yalnızca Pro üyeler için",
        CatalogCategory.Furniture,
        CatalogTier.ProOnly,
        1,
        100,
        "/sprites/items/pro_gold_trophy.png");
}
