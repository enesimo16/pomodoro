namespace Focus.Application.Features.Streak.DTOs;

public record UserStreakDto(
    int CurrentStreak,
    int LongestStreak,
    DateTime? LastActivityDate,
    int FreezesAvailable,
    DateTime? LastFreezeUsedDate,
    bool IsActiveToday,
    bool IsStreakAtRisk,
    int NextMilestoneDays,
    int NextMilestoneBonus);
