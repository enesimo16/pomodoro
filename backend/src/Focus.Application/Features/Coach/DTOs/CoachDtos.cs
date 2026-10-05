namespace Focus.Application.Features.Coach.DTOs;

using Focus.Domain.Enums;

public record MemoryInsightDto(
    Guid Id,
    bool IsGlobal,
    MemoryCategory Category,
    string Content,
    int Importance,
    double RelevanceScore);

public record CoachAnomalyStatusDto(
    bool HasAnomaly,
    AnomalyType AnomalyType,
    double DailyFatigueScore,
    int WeeklyFocusMinutes,
    int DaysSinceLastSession,
    string StatusSummary,
    string RecommendedAction);

public record CoachChatRequest(string Message);

public record CoachChatResponseDto(
    string Reply,
    AnomalyType DetectedAnomaly,
    double DailyFatigueScore,
    List<MemoryInsightDto> UsedMemories,
    bool GeneratedNewMemory);

public record SessionCheckInRequest(
    Guid SessionId,
    SessionMood Mood,
    int EnergyLevel,
    string? TargetIntent);

public record SessionCheckInDto(
    Guid Id,
    Guid SessionId,
    Guid UserId,
    SessionMood Mood,
    int EnergyLevel,
    string? TargetIntent,
    DateTime CreatedAt);

public record SessionReflectionRequest(
    Guid SessionId,
    int FocusQuality,
    SessionMoodAfter MoodAfter,
    string? DistractionNote);

public record SessionReflectionDto(
    Guid Id,
    Guid SessionId,
    Guid UserId,
    int FocusQuality,
    SessionMoodAfter MoodAfter,
    string? DistractionNote,
    DateTime CreatedAt);
