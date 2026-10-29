using Focus.Domain.Entities;
using Xunit;

namespace Focus.UnitTests.Streak;

public class UserStreakEntityTests
{
    [Fact]
    public void RecordActivity_FirstDay_ShouldStartStreakAt1()
    {
        var userId = Guid.NewGuid();
        var streak = UserStreak.CreateDefault(userId);
        var now = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);

        var result = streak.RecordActivity(now);

        Assert.Equal(1, streak.CurrentStreak);
        Assert.Equal(1, streak.LongestStreak);
        Assert.Equal(now.Date, streak.LastActivityDate);
        Assert.Equal(0, result.BonusCoins);
        Assert.False(result.FreezeUsed);
    }

    [Fact]
    public void RecordActivity_SameDay_ShouldNotIncrement()
    {
        var userId = Guid.NewGuid();
        var streak = UserStreak.CreateDefault(userId);
        var day1 = new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc);
        var day1Later = new DateTime(2026, 10, 4, 18, 0, 0, DateTimeKind.Utc);

        streak.RecordActivity(day1);
        var result = streak.RecordActivity(day1Later);

        Assert.Equal(1, streak.CurrentStreak);
        Assert.Contains("zaten kayıtlı", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecordActivity_ConsecutiveDays_ShouldIncrementStreak()
    {
        var userId = Guid.NewGuid();
        var streak = UserStreak.CreateDefault(userId);
        var day1 = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var day2 = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
        var day3 = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

        streak.RecordActivity(day1);
        streak.RecordActivity(day2);
        var result = streak.RecordActivity(day3);

        Assert.Equal(3, streak.CurrentStreak);
        Assert.Equal(3, streak.LongestStreak);
        Assert.Equal(15, result.BonusCoins); // Day 3 milestone gives 15 bonus coins
    }

    [Fact]
    public void RecordActivity_MissedDayWithFreeze_ShouldUseFreezeAndPreserveStreak()
    {
        var userId = Guid.NewGuid();
        var streak = UserStreak.CreateDefault(userId);
        var day1 = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var day3 = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc); // Missed Oct 2

        streak.RecordActivity(day1);
        Assert.Equal(1, streak.FreezesAvailable);

        var result = streak.RecordActivity(day3);

        Assert.Equal(2, streak.CurrentStreak);
        Assert.Equal(0, streak.FreezesAvailable);
        Assert.True(result.FreezeUsed);
        Assert.Equal(new DateTime(2026, 10, 2), streak.LastFreezeUsedDate);
    }

    [Fact]
    public void RecordActivity_MissedDayWithoutFreeze_ShouldResetStreakToOne()
    {
        var userId = Guid.NewGuid();
        var streak = UserStreak.CreateDefault(userId);
        var day1 = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var day4 = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc); // Missed 2 days (Oct 2 & 3)

        streak.RecordActivity(day1);
        var result = streak.RecordActivity(day4);

        Assert.Equal(1, streak.CurrentStreak);
        Assert.False(result.FreezeUsed);
        Assert.Contains("sıfırlandı", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddFreeze_MaxTwoEnforced()
    {
        var userId = Guid.NewGuid();
        var streak = UserStreak.CreateDefault(userId); // starts with 1
        Assert.Equal(1, streak.FreezesAvailable);

        var added1 = streak.AddFreeze(1);
        Assert.True(added1);
        Assert.Equal(2, streak.FreezesAvailable);

        var added2 = streak.AddFreeze(1);
        Assert.False(added2);
        Assert.Equal(2, streak.FreezesAvailable);
    }

    [Fact]
    public void OvernightFreeze_MarksMissedDayAsCovered_SoNextActivityKeepsStreakWithoutSecondFreeze()
    {
        var streak = UserStreak.CreateDefault(Guid.NewGuid());
        var day1 = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        streak.RecordActivity(day1);
        streak.RecordActivity(day1.AddDays(1));
        streak.AddFreeze(1);
        Assert.Equal(2, streak.CurrentStreak);
        Assert.Equal(2, streak.FreezesAvailable);

        // Oct 3 hic odaklanilmadi; gece isi Oct 4 00:30'da calisir
        var (freezeUsed, reset) = streak.ApplyOvernightFreezeOrReset(new DateTime(2026, 10, 4, 0, 30, 0, DateTimeKind.Utc));
        Assert.True(freezeUsed);
        Assert.False(reset);
        Assert.Equal(1, streak.FreezesAvailable);
        Assert.Equal(new DateTime(2026, 10, 3), streak.LastActivityDate);

        var result = streak.RecordActivity(new DateTime(2026, 10, 4, 9, 0, 0, DateTimeKind.Utc));

        Assert.False(result.FreezeUsed);
        Assert.Equal(3, streak.CurrentStreak);
        Assert.Equal(1, streak.FreezesAvailable);
    }

    [Fact]
    public void OvernightFreeze_WithSingleFreeze_KeepsStreakOnNextActivity()
    {
        var streak = UserStreak.CreateDefault(Guid.NewGuid());
        streak.RecordActivity(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

        streak.ApplyOvernightFreezeOrReset(new DateTime(2026, 10, 3, 0, 30, 0, DateTimeKind.Utc));
        var result = streak.RecordActivity(new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc));

        Assert.False(result.FreezeUsed);
        Assert.Equal(2, streak.CurrentStreak);
        Assert.Equal(0, streak.FreezesAvailable);
    }

    [Fact]
    public void OvernightJob_IsIdempotentWithinTheSameNight()
    {
        var streak = UserStreak.CreateDefault(Guid.NewGuid());
        streak.AddFreeze(1);
        streak.RecordActivity(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));
        var night = new DateTime(2026, 10, 3, 0, 30, 0, DateTimeKind.Utc);

        streak.ApplyOvernightFreezeOrReset(night);
        var second = streak.ApplyOvernightFreezeOrReset(night.AddHours(1));

        Assert.False(second.FreezeUsed);
        Assert.False(second.Reset);
        Assert.Equal(1, streak.FreezesAvailable);
    }

    [Fact]
    public void ExtendedAbsence_BurnsOneFreezePerMissedDayThenResetsAndStops()
    {
        var streak = UserStreak.CreateDefault(Guid.NewGuid());
        streak.AddFreeze(1);
        streak.RecordActivity(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

        // Oct 2, 3, 4 ve 5 kacirildi; gece isleri Oct 3..6 arasi calisir
        var night1 = streak.ApplyOvernightFreezeOrReset(new DateTime(2026, 10, 3, 0, 30, 0, DateTimeKind.Utc));
        var night2 = streak.ApplyOvernightFreezeOrReset(new DateTime(2026, 10, 4, 0, 30, 0, DateTimeKind.Utc));
        var night3 = streak.ApplyOvernightFreezeOrReset(new DateTime(2026, 10, 5, 0, 30, 0, DateTimeKind.Utc));
        var night4 = streak.ApplyOvernightFreezeOrReset(new DateTime(2026, 10, 6, 0, 30, 0, DateTimeKind.Utc));

        Assert.True(night1.FreezeUsed);
        Assert.True(night2.FreezeUsed);
        Assert.True(night3.Reset);
        Assert.False(night4.FreezeUsed);
        Assert.False(night4.Reset);
        Assert.Equal(0, streak.FreezesAvailable);
        Assert.Equal(0, streak.CurrentStreak);
    }

    [Fact]
    public void RecordActivity_AfterReset_DoesNotBurnAFreezeForAResetStreak()
    {
        var streak = UserStreak.CreateDefault(Guid.NewGuid());
        streak.RecordActivity(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));
        streak.ApplyOvernightFreezeOrReset(new DateTime(2026, 10, 3, 0, 30, 0, DateTimeKind.Utc));
        streak.ApplyOvernightFreezeOrReset(new DateTime(2026, 10, 4, 0, 30, 0, DateTimeKind.Utc));
        Assert.Equal(0, streak.CurrentStreak);
        streak.AddFreeze(1);

        var result = streak.RecordActivity(new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc));

        Assert.False(result.FreezeUsed);
        Assert.Equal(1, streak.CurrentStreak);
        Assert.Equal(1, streak.FreezesAvailable);
    }
}
