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

    private static DbContextOptions<ApplicationDbContext> CreateNpgsqlOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=focus_model_test;Username=test;Password=test", o => o.UseVector())
            .Options;
    }
}
