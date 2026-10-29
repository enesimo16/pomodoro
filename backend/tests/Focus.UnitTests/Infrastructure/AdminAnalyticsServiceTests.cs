using FluentAssertions;
using Focus.Domain.Entities;
using Focus.Infrastructure.Persistence;
using Focus.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class AdminAnalyticsServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task EconomyAnalytics_CategorySpendCoversAllPurchasesNotOnlyTopTen()
    {
        var context = CreateContext();
        var user = User.CreateGuest("Alici");
        context.Users.Add(user);
        var catalog = await context.CatalogItems.AsNoTracking().ToListAsync();
        catalog.Count.Should().BeGreaterThan(10);

        foreach (var item in catalog)
        {
            context.UserInventoryItems.Add(new UserInventoryItem(user.Id, item.Id));
        }

        await context.SaveChangesAsync();

        var economy = await new AdminAnalyticsService(context).GetEconomyAnalyticsAsync();

        economy.TopPurchasedItems.Should().HaveCount(10);
        var expected = catalog
            .GroupBy(c => c.Category.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(c => (long)c.CoinPrice));
        economy.CategorySpendBreakdown.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task UserAnalytics_ReturnsTopUsersAndLevelDistribution()
    {
        var context = CreateContext();
        context.Users.AddRange(User.CreateGuest("A"), User.CreateGuest("B"), User.CreateGuest("C"));
        await context.SaveChangesAsync();

        var analytics = await new AdminAnalyticsService(context).GetUserAnalyticsAsync();

        analytics.LevelDistribution["Sv. 1-5"].Should().Be(3);
        analytics.TopUsersByXp.Should().HaveCount(3);
    }

    [Fact]
    public async Task SystemHealth_ReportsRedisAsNotConfiguredAndHealthyDatabase()
    {
        var context = CreateContext();

        var health = await new AdminAnalyticsService(context).GetSystemHealthAsync();

        health.DatabaseStatus.Should().Be("Healthy");
        health.RedisStatus.Should().Be("NotConfigured");
    }

    [Fact]
    public async Task SystemHealth_WhenDatabaseUnavailable_ReportsDegraded()
    {
        var context = CreateContext();
        await context.DisposeAsync();

        var health = await new AdminAnalyticsService(context).GetSystemHealthAsync();

        health.DatabaseStatus.Should().Be("Degraded");
        health.TotalAgentMemories.Should().Be(0);
    }
}
