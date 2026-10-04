using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserAvatar> UserAvatars { get; }
    DbSet<PixelRoom> PixelRooms { get; }
    DbSet<RoomItem> RoomItems { get; }
    DbSet<UserPreferences> UserPreferences { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserExternalLogin> UserExternalLogins { get; }
    DbSet<CoinLedgerEntry> CoinLedgerEntries { get; }
    DbSet<FocusSession> FocusSessions { get; }
    DbSet<CatalogItem> CatalogItems { get; }
    DbSet<UserInventoryItem> UserInventoryItems { get; }
    DbSet<UserStreak> UserStreaks { get; }
    DbSet<StudyRoom> StudyRooms { get; }
    DbSet<RoomMember> RoomMembers { get; }
    DbSet<RoomInvitation> RoomInvitations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
