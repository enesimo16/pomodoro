using Focus.Admin.Pages.Users;
using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Focus.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Focus.UnitTests.Admin;

public class AdminCoinGrantTests
{
    private static IndexModel CreatePage(IApplicationDbContext context)
    {
        return new IndexModel(context, NullLogger<IndexModel>.Instance);
    }

    private static async Task<User> AddUserAsync(IApplicationDbContext context, int startingCoins = 0)
    {
        var user = User.CreateGuest("AdminTarget");
        context.Users.Add(user);
        if (startingCoins != 0)
        {
            context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.InitialBonus, startingCoins));
        }

        await context.SaveChangesAsync();
        return user;
    }

    [Theory]
    [InlineData(250)]
    [InlineData(-40)]
    public async Task GrantCoins_RecordsAdminAdjustmentLedgerEntry(int amount)
    {
        using var context = TestDbContextFactory.Create();
        var user = await AddUserAsync(context, 100);

        await CreatePage(context).OnPostGrantCoinsAsync(user.Id, amount, "test");

        var entry = await context.CoinLedgerEntries.SingleAsync(c => c.UserId == user.Id && c.Reason == CoinTransactionReason.AdminAdjustment);
        Assert.Equal(amount, entry.Coins);
    }

    [Fact]
    public async Task GrantCoins_WithZeroAmount_WritesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var user = await AddUserAsync(context);

        await CreatePage(context).OnPostGrantCoinsAsync(user.Id, 0, "test");

        Assert.Empty(context.CoinLedgerEntries.Where(c => c.UserId == user.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GrantCoins_WithoutReason_WritesNothing(string? reason)
    {
        using var context = TestDbContextFactory.Create();
        var user = await AddUserAsync(context);

        var page = CreatePage(context);
        await page.OnPostGrantCoinsAsync(user.Id, 50, reason);

        Assert.Empty(context.CoinLedgerEntries.Where(c => c.UserId == user.Id));
        Assert.NotNull(page.StatusMessage);
    }

    [Theory]
    [InlineData(100_001)]
    [InlineData(-100_001)]
    [InlineData(int.MinValue)]
    public async Task GrantCoins_AboveCap_WritesNothing(int amount)
    {
        using var context = TestDbContextFactory.Create();
        var user = await AddUserAsync(context, 100);

        await CreatePage(context).OnPostGrantCoinsAsync(user.Id, amount, "test");

        Assert.Empty(context.CoinLedgerEntries.Where(c => c.Reason == CoinTransactionReason.AdminAdjustment));
    }

    [Fact]
    public async Task GrantCoins_ThatWouldMakeBalanceNegative_WritesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var user = await AddUserAsync(context, 30);

        await CreatePage(context).OnPostGrantCoinsAsync(user.Id, -31, "test");

        Assert.Empty(context.CoinLedgerEntries.Where(c => c.Reason == CoinTransactionReason.AdminAdjustment));
    }

    [Fact]
    public async Task GrantCoins_ForUnknownUser_WritesNothing()
    {
        using var context = TestDbContextFactory.Create();

        var page = CreatePage(context);
        await page.OnPostGrantCoinsAsync(Guid.NewGuid(), 10, "test");

        Assert.Empty(context.CoinLedgerEntries);
        Assert.Equal("Kullanıcı bulunamadı.", page.StatusMessage);
    }

    [Fact]
    public async Task AddXp_AddsXpToExistingUser()
    {
        using var context = TestDbContextFactory.Create();
        var user = await AddUserAsync(context);

        await CreatePage(context).OnPostAddXpAsync(user.Id, 500);

        Assert.Equal(500, (await context.Users.SingleAsync(u => u.Id == user.Id)).CurrentXp);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1_000_001)]
    [InlineData(long.MaxValue)]
    public async Task AddXp_OutOfRange_ChangesNothing(long xp)
    {
        using var context = TestDbContextFactory.Create();
        var user = await AddUserAsync(context);

        await CreatePage(context).OnPostAddXpAsync(user.Id, xp);

        Assert.Equal(0, (await context.Users.SingleAsync(u => u.Id == user.Id)).CurrentXp);
    }

    [Fact]
    public async Task AddXp_ForUnknownUser_DoesNotThrow()
    {
        using var context = TestDbContextFactory.Create();

        var page = CreatePage(context);
        await page.OnPostAddXpAsync(Guid.NewGuid(), 10);

        Assert.Equal("Kullanıcı bulunamadı.", page.StatusMessage);
    }
}
