namespace Focus.Application.Features.Stats.DTOs;

public record DailyFocusStatDto(
    DateTime Date,
    string DayName,
    int Minutes,
    int SessionCount);

public record UserStatsDto(
    int TotalFocusMinutes,
    int TotalCompletedSessions,
    int TodayFocusMinutes,
    int TodaySessionCount,
    int ThisWeekFocusMinutes,
    int ThisMonthFocusMinutes,
    int CurrentStreak,
    int LongestStreak,
    int CurrentLevel,
    long CurrentXp,
    long XpRequiredForCurrentLevel,
    long XpRequiredForNextLevel,
    double LevelProgressPercentage,
    int TotalCoins,
    int InventoryItemCount,
    List<DailyFocusStatDto> Last7Days);
