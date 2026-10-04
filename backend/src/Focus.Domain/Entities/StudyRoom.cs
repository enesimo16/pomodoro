using Focus.Domain.Common;
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public class StudyRoom : BaseEntity<Guid>, IAggregateRoot
{
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
            MaxCapacity = isOwnerPro ? 4 : 2,
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
        int capacity = 20)
    {
        return new StudyRoom
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant().Trim(),
            Name = name,
            Description = description,
            Type = StudyRoomType.PublicLibrary,
            OwnerUserId = null,
            MaxCapacity = capacity,
            IsPrivate = false,
            ThemeId = themeId,
            MusicTrackId = musicTrackId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static StudyRoom CreateShopMarket(string name, string code, string description)
    {
        return new StudyRoom
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant().Trim(),
            Name = name,
            Description = description,
            Type = StudyRoomType.ShopMarket,
            OwnerUserId = null,
            MaxCapacity = 50,
            IsPrivate = false,
            ThemeId = "cozy_bazaar",
            MusicTrackId = "market_chill",
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool CanJoin(bool isOwnerPro, out string? errorMessage)
    {
        errorMessage = null;

        if (Type == StudyRoomType.PrivateHome)
        {
            if (!isOwnerPro && _members.Count >= 2)
            {
                errorMessage = "Ücretsiz piksel odalarda en fazla 2 kişi (Oda sahibi + 1 misafir) bulunabilir. Daha fazla arkadaşınızla çalışmak için Premium hesaba geçmelisiniz.";
                return false;
            }

            if (isOwnerPro && _members.Count >= 4)
            {
                errorMessage = "Özel çalışma odası maksimum 4 kişilik kapasite sınırına ulaştı.";
                return false;
            }
        }

        if (_members.Count >= MaxCapacity)
        {
            errorMessage = "Oda kapasitesi dolu.";
            return false;
        }

        return true;
    }

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
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            _members.Remove(member);
        }
    }

    public bool AssignSeat(Guid userId, int seatIndex)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return false;

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
            MaxCapacity = isOwnerPro ? 4 : 2;
        }
    }

    public int CalculateCoWorkingBonusPercent()
    {
        var focusingCount = _members.Count(m => m.IsFocusing);
        return focusingCount switch
        {
            2 => 10,
            3 => 15,
            >= 4 => 20,
            _ => 0
        };
    }
}
