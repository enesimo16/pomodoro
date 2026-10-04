using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserAvatar> UserAvatars => Set<UserAvatar>();
    public DbSet<PixelRoom> PixelRooms => Set<PixelRoom>();
    public DbSet<RoomItem> RoomItems => Set<RoomItem>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();
    public DbSet<CoinLedgerEntry> CoinLedgerEntries => Set<CoinLedgerEntry>();
    public DbSet<FocusSession> FocusSessions => Set<FocusSession>();
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<UserInventoryItem> UserInventoryItems => Set<UserInventoryItem>();
    public DbSet<UserStreak> UserStreaks => Set<UserStreak>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL pgvector eklentisi (sadece Npgsql saglayicisinda)
        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("vector");
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Baslangic esya katalog verileri
        CatalogSeeder.SeedCatalogItems(modelBuilder);
    }
}
