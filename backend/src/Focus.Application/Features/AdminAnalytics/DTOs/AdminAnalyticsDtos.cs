namespace Focus.Application.Features.AdminAnalytics.DTOs;

public record PlatformOverviewDto(
    int TotalUsers,
    int GuestUsers,
    int RegisteredUsers,
    int ActiveUsersToday,
    int ActiveUsersThisWeek,
    int ActiveUsersThisMonth,
    long TotalFocusMinutes,
    int TotalSessions,
    int CompletedSessions,
    int AbandonedSessions,
    double CompletionRate
);

public record TopUserSummaryDto(
    Guid UserId,
    string DisplayName,
    int Level,
    long CurrentXp,
    int FocusCoins,
    int CurrentStreak
);

public record UserAnalyticsDto(
    int TotalAvatars,
    Dictionary<string, int> LevelDistribution,
    IReadOnlyList<TopUserSummaryDto> TopUsersByXp,
    IReadOnlyList<TopUserSummaryDto> TopUsersByStreak
);

public record TopItemPurchaseDto(
    string ItemId,
    string Name,
    string Category,
    int Price,
    int TotalPurchased
);

public record EconomyAnalyticsDto(
    long TotalCirculatingCoins,
    long TotalCoinsEarned,
    long TotalCoinsSpent,
    IReadOnlyList<TopItemPurchaseDto> TopPurchasedItems,
    Dictionary<string, long> CategorySpendBreakdown
);

public record SystemHealthDto(
    string DatabaseStatus,
    double DatabaseLatencyMs,
    string RedisStatus,
    int TotalAgentMemories,
    int PersonalMemoriesCount,
    int GlobalMemoriesCount,
    int TotalStudyRooms,
    int ActiveStudyRooms,
    DateTime ServerTimeUtc
);
