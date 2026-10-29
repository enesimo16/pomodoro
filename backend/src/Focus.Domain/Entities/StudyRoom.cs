using Focus.Domain.Common;
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public class StudyRoom : BaseEntity<Guid>, IAggregateRoot
{
    public const int CoWorkingBonusPercent = 15;
    public const int FreeHomeCapacity = 2;
    public const int ProHomeCapacity = 4;

    private readonly List<RoomMember> _members = new();
    private readonly List<RoomInvitation> _invitations = new();

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public StudyRoomType Type { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public int MaxCapacity { get; private set; }
    public bool IsPrivate { get; private set; }
    public string? AccessCode { get; private set; }
    public string? ThemeId { get; private set; }
    public string? MusicTrackId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

    // Navigation
    public User? OwnerUser { get; private set; }
    public IReadOnlyCollection<RoomMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<RoomInvitation> Invitations => _invitations.AsReadOnly();

    // EF Core constructor
    private StudyRoom() { }

    public static StudyRoom CreatePrivateRoom(Guid ownerUserId, string name, string code, bool isOwnerPro = false)
    {
        return new StudyRoom
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant().Trim(),
            Name = name,
            Description = $"{name} - Kişisel Piksel Çalışma Alanı",
            Type = StudyRoomType.PrivateHome,
            OwnerUserId = ownerUserId,
            MaxCapacity = HomeCapacityFor(isOwnerPro),
            IsPrivate = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static StudyRoom CreatePublicLibrary(
        string name,
        string code,
        string description,
        string? themeId = null,
        string? musicTrackId = null,
        int capacity = 20,
        Guid? id = null,
        DateTime? createdAt = null)
    {
        return new StudyRoom
        {
            Id = id ?? Guid.NewGuid(),
            ConcurrencyStamp = id ?? Guid.NewGuid(),
            Code = code.ToUpperInvariant().Trim(),
            Name = name,
            Description = description,
            Type = StudyRoomType.PublicLibrary,
            OwnerUserId = null,
            MaxCapacity = capacity,
            IsPrivate = false,
            ThemeId = themeId,
            MusicTrackId = musicTrackId,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    public static StudyRoom CreateShopMarket(
        string name,
        string code,
        string description,
        Guid? id = null,
        DateTime? createdAt = null)
    {
        return new StudyRoom
        {
            Id = id ?? Guid.NewGuid(),
            ConcurrencyStamp = id ?? Guid.NewGuid(),
            Code = code.ToUpperInvariant().Trim(),
            Name = name,
            Description = description,
            Type = StudyRoomType.ShopMarket,
            OwnerUserId = null,
            MaxCapacity = 50,
            IsPrivate = false,
            ThemeId = "cozy_bazaar",
            MusicTrackId = "market_chill",
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    public static int HomeCapacityFor(bool isOwnerPro) => isOwnerPro ? ProHomeCapacity : FreeHomeCapacity;

    // Ozel odalarda kapasite her zaman oda sahibinin guncel Pro durumundan hesaplanir
    public int EffectiveCapacity(bool isOwnerPro)
    {
        return Type == StudyRoomType.PrivateHome ? HomeCapacityFor(isOwnerPro) : MaxCapacity;
    }

    public bool CanJoin(bool isOwnerPro, out string? errorMessage)
    {
        errorMessage = null;

        if (_members.Count < EffectiveCapacity(isOwnerPro))
        {
            return true;
        }

        errorMessage = Type == StudyRoomType.PrivateHome
            ? isOwnerPro
                ? "Özel çalışma odası maksimum 4 kişilik kapasite sınırına ulaştı."
                : "Ücretsiz piksel odalarda en fazla 2 kişi (Oda sahibi + 1 misafir) bulunabilir. Daha fazla arkadaşınızla çalışmak için Premium hesaba geçmelisiniz."
            : "Oda kapasitesi dolu.";
        return false;
    }

    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public RoomMember AddMember(Guid userId, int? seatIndex = null)
    {
        var existing = _members.FirstOrDefault(m => m.UserId == userId);
        if (existing != null)
        {
            existing.Touch();
            if (seatIndex.HasValue) existing.SitAtSeat(seatIndex.Value);
            return existing;
        }

        var member = new RoomMember(Id, userId, seatIndex);
        _members.Add(member);
        ConcurrencyStamp = Guid.NewGuid();
        return member;
    }

    public RoomMember? RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return null;

        _members.Remove(member);
        ConcurrencyStamp = Guid.NewGuid();
        return member;
    }

    public bool IsValidSeat(int seatIndex) => seatIndex >= 0 && seatIndex < MaxCapacity;

    public bool AssignSeat(Guid userId, int seatIndex)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null || !IsValidSeat(seatIndex)) return false;

        // Baska biri o koltukta oturuyor mu?
        if (_members.Any(m => m.UserId != userId && m.SeatIndex == seatIndex))
        {
            return false;
        }

        member.SitAtSeat(seatIndex);
        return true;
    }

    public void UpdateProCapacity(bool isOwnerPro)
    {
        if (Type == StudyRoomType.PrivateHome)
        {
            MaxCapacity = HomeCapacityFor(isOwnerPro);
        }
    }

    public int CalculateCoWorkingBonusPercent()
    {
        return CoWorkingBonusPercentFor(_members.Count(m => m.IsFocusing));
    }

    // docs/algoritmalar.md 3.1: odada bir arkadasla es zamanli odaklanilirsa her iki tarafa +%15
    public static int CoWorkingBonusPercentFor(int focusingCount)
    {
        return focusingCount >= 2 ? CoWorkingBonusPercent : 0;
    }
}
