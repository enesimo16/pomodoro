using FluentAssertions;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class PersistenceModelTests
{
    [Fact]
    public void PublicRoomSeed_ShouldUseFixedIdsAndTimestamps()
    {
        var rooms = PublicRoomSeeder.InitialPublicRooms;

        rooms.Select(r => r.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
        rooms.Select(r => r.CreatedAt).Distinct().Should().ContainSingle();
        rooms.Should().OnlyContain(r => r.CreatedAt.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public void CatalogSeed_ShouldUseFixedTimestamps()
    {
        using var context = new ApplicationDbContext(CreateNpgsqlOptions());

        var seededItems = context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model
            .FindEntityType(typeof(Focus.Domain.Entities.CatalogItem))!
            .GetSeedData();

        seededItems.Select(s => s["CreatedAt"]).Distinct().Should().ContainSingle();
    }

    [Fact]
    public void Model_ShouldMatchLatestMigrationSnapshot()
    {
        using var context = new ApplicationDbContext(CreateNpgsqlOptions());

        context.Database.HasPendingModelChanges().Should().BeFalse(
            "model degisiklikleri bir migrasyonla yansitilmali ve tohum verisi deterministik olmali");
    }

    [Theory]
    [InlineData(typeof(Focus.Domain.Entities.FocusSession))]
    [InlineData(typeof(Focus.Domain.Entities.User))]
    [InlineData(typeof(Focus.Domain.Entities.UserStreak))]
    public void WalletAffectingEntities_ShouldHaveConcurrencyToken(Type entityType)
    {
        using var context = new ApplicationDbContext(CreateNpgsqlOptions());

        var property = GetModel(context).FindEntityType(entityType)!.FindProperty("ConcurrencyStamp");

        property.Should().NotBeNull();
        property!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void FocusSessions_ShouldAllowOnlyOneActiveSessionPerUser()
    {
        using var context = new ApplicationDbContext(CreateNpgsqlOptions());

        var index = GetModel(context).FindEntityType(typeof(Focus.Domain.Entities.FocusSession))!
            .GetIndexes()
            .Single(i => i.IsUnique);

        index.Properties.Select(p => p.Name).Should().Equal("UserId");
        index.GetFilter().Should().Contain("Running").And.Contain("Paused");
    }

    [Fact]
    public void CoinLedger_ShouldMakeSessionRewardsIdempotent()
    {
        using var context = new ApplicationDbContext(CreateNpgsqlOptions());

        var index = GetModel(context).FindEntityType(typeof(Focus.Domain.Entities.CoinLedgerEntry))!
            .GetIndexes()
            .Single(i => i.IsUnique);

        index.Properties.Select(p => p.Name).Should().Equal("UserId", "SessionId", "Reason");
        index.GetFilter().Should().Contain("SessionId").And.Contain("IS NOT NULL");
    }

    [Fact]
    public void Inventory_ShouldNotAllowTheSameCatalogItemTwice()
    {
        using var context = new ApplicationDbContext(CreateNpgsqlOptions());

        var index = GetModel(context).FindEntityType(typeof(Focus.Domain.Entities.UserInventoryItem))!
            .GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "UserId", "CatalogItemId" }));

        index.IsUnique.Should().BeTrue();
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IModel GetModel(ApplicationDbContext context)
        => context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;

    private static DbContextOptions<ApplicationDbContext> CreateNpgsqlOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=focus_model_test;Username=test;Password=test", o => o.UseVector())
            .Options;
    }
}
