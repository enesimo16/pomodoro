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
    public DbSet<StudyRoom> StudyRooms => Set<StudyRoom>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();
    public DbSet<RoomInvitation> RoomInvitations => Set<RoomInvitation>();
    public DbSet<AgentMemory> AgentMemories => Set<AgentMemory>();
    public DbSet<SessionCheckIn> SessionCheckIns => Set<SessionCheckIn>();
    public DbSet<SessionReflection> SessionReflections => Set<SessionReflection>();
    public DbSet<RoomGuestbookEntry> RoomGuestbookEntries => Set<RoomGuestbookEntry>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserDeviceSession> UserDeviceSessions => Set<UserDeviceSession>();
    public DbSet<SavedAtmosphere> SavedAtmospheres => Set<SavedAtmosphere>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL pgvector eklentisi (sadece Npgsql saglayicisinda)
        var valueComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<float[]>(
            (c1, c2) => (c1 != null && c2 != null) ? c1.SequenceEqual(c2) : c1 == c2,
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToArray());

        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<AgentMemory>(b =>
            {
                b.Property(m => m.Embedding)
                    .HasColumnType("vector(768)")
                    .HasConversion(v => new Pgvector.Vector(v), v => v.ToArray(), valueComparer)
                    .IsRequired();

                b.HasIndex(m => m.Embedding)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops");
            });
        }
        else
        {
            modelBuilder.Entity<AgentMemory>(b =>
            {
                b.Property(m => m.Embedding)
                    .HasConversion(
                        v => string.Join(",", v),
                        s => s.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray(),
                        valueComparer)
                    .IsRequired();
            });
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Baslangic esya katalog verileri
        CatalogSeeder.SeedCatalogItems(modelBuilder);

        // Genel kutuphaneler ve piksel carsi
        PublicRoomSeeder.SeedPublicRooms(modelBuilder);
    }
}
