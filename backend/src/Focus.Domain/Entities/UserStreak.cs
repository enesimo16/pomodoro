using Focus.Domain.Common;

namespace Focus.Domain.Entities;

public class UserStreak : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateTime? LastActivityDate { get; private set; }
    public int FreezesAvailable { get; private set; } = 1;
    public DateTime? LastFreezeUsedDate { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    // EF Core constructor
    private UserStreak() { }

    public static UserStreak CreateDefault(Guid userId)
    {
        return new UserStreak
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStreak = 0,
            LongestStreak = 0,
            FreezesAvailable = 1,
            LastActivityDate = null,
            LastFreezeUsedDate = null
        };
    }

    public (int NewStreak, int BonusCoins, bool FreezeUsed, string Message) RecordActivity(DateTime activityTime)
    {
        var today = activityTime.Date;

        if (LastActivityDate.HasValue && LastActivityDate.Value.Date == today)
        {
            return (CurrentStreak, 0, false, "Bugünkü odaklanma serisi zaten kayıtlı.");
        }

        if (!LastActivityDate.HasValue)
        {
            CurrentStreak = 1;
            LongestStreak = 1;
            LastActivityDate = today;
            return (CurrentStreak, 0, false, "Tebrikler! İlk odaklanma gününüz kaydedildi.");
        }

        var yesterday = today.AddDays(-1);
        if (LastActivityDate.Value.Date == yesterday)
        {
            CurrentStreak++;
            if (CurrentStreak > LongestStreak)
            {
                LongestStreak = CurrentStreak;
            }

            LastActivityDate = today;
            var bonus = CalculateMilestoneBonus(CurrentStreak);
            var msg = bonus > 0
                ? $"Harika! {CurrentStreak} günlük seri! +{bonus} Streak Bonusu kazandınız!"
                : $"Seri devam ediyor: {CurrentStreak} gün!";

            return (CurrentStreak, bonus, false, msg);
        }

        // 1 gün kaçırılmışsa ve dondurucu varsa dondurucu devreye girer
        if (LastActivityDate.Value.Date == today.AddDays(-2) && FreezesAvailable > 0)
        {
            FreezesAvailable--;
            LastFreezeUsedDate = yesterday;
            CurrentStreak++;
            if (CurrentStreak > LongestStreak)
            {
                LongestStreak = CurrentStreak;
            }

            LastActivityDate = today;
            var bonus = CalculateMilestoneBonus(CurrentStreak);
            return (CurrentStreak, bonus, true, $"Seri Dondurucu Devreye Girdi! Dün kaçırılan gün korundu. Mevcut seri: {CurrentStreak} gün.");
        }

        // Seri sıfırlandı
        CurrentStreak = 1;
        LastActivityDate = today;
        return (CurrentStreak, 0, false, "Seri sıfırlandı. Yeni seriniz 1. günden başladı.");
    }

    public bool AddFreeze(int count = 1)
    {
        if (FreezesAvailable >= 2) return false;

        FreezesAvailable = Math.Min(2, FreezesAvailable + count);
        return true;
    }

    public (bool FreezeUsed, bool Reset) ApplyOvernightFreezeOrReset(DateTime checkDate)
    {
        var yesterday = checkDate.Date.AddDays(-1);
        if (!LastActivityDate.HasValue || LastActivityDate.Value.Date >= yesterday)
        {
            return (false, false);
        }

        if (CurrentStreak > 0)
        {
            if (FreezesAvailable > 0 && LastFreezeUsedDate?.Date != yesterday)
            {
                FreezesAvailable--;
                LastFreezeUsedDate = yesterday;
                return (true, false);
            }

            CurrentStreak = 0;
            return (false, true);
        }

        return (false, false);
    }

    private static int CalculateMilestoneBonus(int streakDays)
    {
        return streakDays switch
        {
            3 => 15,
            7 => 50,
            14 => 120,
            30 => 300,
            _ => 0
        };
    }
}
