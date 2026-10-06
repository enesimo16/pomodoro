using Focus.Domain.Enums;

namespace Focus.Application.Features.Wrapped.DTOs;

public record FocusWrappedDto(
    string Period,
    DateTime StartDate,
    DateTime EndDate,
    int TotalFocusMinutes,
    int TotalSessionsCompleted,
    double CompletionRate,
    string TopDayOfWeek,
    string PeakTimeOfDay,
    Chronotype Chronotype,
    string ChronotypeName,
    string TopTheme,
    int CoinsEarned,
    int XpEarned,
    int CurrentStreak,
    int LongestStreak,
    string SummaryInsight
);
