using Focus.Admin.Pages.Users;
using Focus.Domain.Entities;
using Focus.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Focus.UnitTests.Admin;

public class AdminCoinGrantTests
{
    [Theory]
    [InlineData(250)]
    [InlineData(-40)]
    public async Task GrantCoins_RecordsAdminAdjustmentLedgerEntry(int amount)
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("AdminTarget");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var page = new IndexModel(context);
        await page.OnPostGrantCoinsAsync(user.Id, amount, "test");

        var entry = await context.CoinLedgerEntries.SingleAsync(c => c.UserId == user.Id);
        Assert.Equal(CoinTransactionReason.AdminAdjustment, entry.Reason);
        Assert.Equal(amount, entry.Coins);
    }

    [Fact]
    public async Task GrantCoins_WithZeroAmount_WritesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("AdminTarget");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var page = new IndexModel(context);
        await page.OnPostGrantCoinsAsync(user.Id, 0, null);

        Assert.Empty(context.CoinLedgerEntries.Where(c => c.UserId == user.Id));
    }
}
