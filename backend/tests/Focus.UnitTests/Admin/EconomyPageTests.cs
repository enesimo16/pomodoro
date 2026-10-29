using Focus.Admin.Pages.Economy;
using Focus.Domain.Entities;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Admin;

public class EconomyPageTests
{
    private static async Task<(Focus.Infrastructure.Persistence.ApplicationDbContext Context, User Ayse, User Mehmet)> SeedAsync()
    {
        var context = TestDbContextFactory.Create();
        var ayse = User.CreateGuest("Ayse");
        var mehmet = User.CreateGuest("Mehmet");
        context.Users.AddRange(ayse, mehmet);
        context.CoinLedgerEntries.AddRange(
            new CoinLedgerEntry(ayse.Id, CoinTransactionReason.SessionReward, 40),
            new CoinLedgerEntry(ayse.Id, CoinTransactionReason.ItemPurchase, -15),
            new CoinLedgerEntry(mehmet.Id, CoinTransactionReason.InitialBonus, 50));
        await context.SaveChangesAsync();
        return (context, ayse, mehmet);
    }

    [Fact]
    public async Task Search_MatchesUserNameInTheQuery()
    {
        var (context, _, _) = await SeedAsync();
        using var _ = context;
        var page = new IndexModel(context) { Search = "ayse" };

        await page.OnGetAsync();

        Assert.Equal(2, page.Entries.Count);
        Assert.All(page.Entries, e => Assert.Equal("Ayse", e.UserName));
    }

    [Fact]
    public async Task Search_MatchesReasonName()
    {
        var (context, _, _) = await SeedAsync();
        using var _ = context;
        var page = new IndexModel(context) { Search = "initial" };

        await page.OnGetAsync();

        var entry = Assert.Single(page.Entries);
        Assert.Equal(CoinTransactionReason.InitialBonus, entry.Reason);
    }

    [Fact]
    public async Task Filter_SpentReturnsOnlyNegativeEntries()
    {
        var (context, _, _) = await SeedAsync();
        using var _ = context;
        var page = new IndexModel(context) { Filter = "spent" };

        await page.OnGetAsync();

        Assert.All(page.Entries, e => Assert.True(e.Coins < 0));
    }

    [Fact]
    public async Task Paging_SecondPageHoldsTheOverflow()
    {
        var context = TestDbContextFactory.Create();
        using var _ = context;
        var user = User.CreateGuest("Pager");
        context.Users.Add(user);
        for (var i = 0; i < IndexModel.PageSize + 5; i++)
        {
            context.CoinLedgerEntries.Add(new CoinLedgerEntry(user.Id, CoinTransactionReason.SessionReward, 1));
        }

        await context.SaveChangesAsync();

        var first = new IndexModel(context);
        await first.OnGetAsync();
        var second = new IndexModel(context) { PageNumber = 2 };
        await second.OnGetAsync();

        Assert.Equal(IndexModel.PageSize, first.Entries.Count);
        Assert.True(first.HasNextPage);
        Assert.Equal(5, second.Entries.Count);
        Assert.False(second.HasNextPage);
    }
}
