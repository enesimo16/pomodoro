using Focus.Domain.Common;
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public class User : BaseEntity<Guid>, IAggregateRoot
{
    private readonly List<RefreshToken> _refreshTokens = new();
    private readonly List<UserExternalLogin> _externalLogins = new();
    private readonly List<UserInventoryItem> _inventoryItems = new();

    public string DisplayName { get; private set; } = null!;
    public string? Email { get; private set; }
    public bool IsGuest { get; private set; }
    public bool IsPro { get; private set; }
    public int Level { get; private set; } = 1;
    public long CurrentXp { get; private set; }
    public string Locale { get; private set; } = "tr-TR";
    public string TimeZoneId { get; private set; } = "Europe/Istanbul";
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Navigation
    public UserAvatar? Avatar { get; private set; }
    public PixelRoom? Room { get; private set; }
    public UserPreferences? Preferences { get; private set; }
    public UserStreak? Streak { get; private set; }
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<UserExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();
    public IReadOnlyCollection<UserInventoryItem> InventoryItems => _inventoryItems.AsReadOnly();

    // EF Core constructor
    private User() { }

    public static User CreateGuest(string? displayName = null, string? locale = null, string? timeZoneId = null)
    {
        var randomSuffix = new Random().Next(1000, 9999);
        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName ?? $"Misafir#{randomSuffix}",
            Email = null,
            IsGuest = true,
            Level = 1,
            CurrentXp = 0,
            Locale = locale ?? "tr-TR",
            TimeZoneId = timeZoneId ?? "Europe/Istanbul",
            CreatedAt = DateTime.UtcNow
        };

        user.Avatar = UserAvatar.CreateDefault(user.Id);
        user.Room = PixelRoom.CreateDefault(user.Id, $"{user.DisplayName}'in Odası");
        user.Preferences = UserPreferences.CreateDefault(user.Id);
        user.Streak = UserStreak.CreateDefault(user.Id);

        return user;
    }

    public static User CreateGoogleUser(
        string email,
        string displayName,
        string providerKey,
        string? locale = null,
        string? timeZoneId = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName,
            Email = email.ToLowerInvariant().Trim(),
            IsGuest = false,
            Level = 1,
            CurrentXp = 0,
            Locale = locale ?? "tr-TR",
            TimeZoneId = timeZoneId ?? "Europe/Istanbul",
            CreatedAt = DateTime.UtcNow
        };

        user.Avatar = UserAvatar.CreateDefault(user.Id);
        user.Room = PixelRoom.CreateDefault(user.Id, $"{displayName}'in Odası");
        user.Preferences = UserPreferences.CreateDefault(user.Id);
        user.Streak = UserStreak.CreateDefault(user.Id);
        user._externalLogins.Add(new UserExternalLogin(user.Id, AuthProvider.Google, providerKey, email));

        return user;
    }

    public void ClaimWithGoogle(string email, string displayName)
    {
        Email = email.ToLowerInvariant().Trim();
        DisplayName = displayName;
        IsGuest = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddXp(long xp)
    {
        if (xp <= 0) return;

        CurrentXp += xp;
        // Level formülü: Level = 1 + floor(sqrt(XP / 100))
        var calculatedLevel = 1 + (int)Math.Floor(Math.Sqrt(CurrentXp / 100.0));
        if (calculatedLevel > Level)
        {
            Level = calculatedLevel;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRefreshToken(string tokenHash, DateTime expiresAt, string? createdByIp)
    {
        _refreshTokens.Add(new RefreshToken(Id, tokenHash, expiresAt, createdByIp));
    }

    public void SetPro(bool isPro)
    {
        IsPro = isPro;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
    }
}
