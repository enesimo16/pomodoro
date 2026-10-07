namespace Focus.Application.Features.Atmosphere.DTOs;

public record GenerateAtmosphereRequestDto(
    string? Prompt,
    string? FocusGoal,
    string? MoodTarget,
    string? PreferredAmbience,
    string? VisualAesthetic,
    string? TimeOfDay
);

public record ApplyAtmosphereRequestDto(
    Guid? RoomId = null,
    string? WallPaperId = null,
    string? FloorId = null,
    int? FocusMinutes = null,
    int? BreakMinutes = null,
    bool? FlowShieldEnabled = null,
    GeneratedAtmosphereDto? Atmosphere = null
);

public record SavedAtmosphereDto(
    Guid Id,
    string Name,
    string Description,
    string Aesthetic,
    string WallColor,
    string FloorColor,
    string AccentLightColor,
    string? WallPaperId,
    string? FloorId,
    string WeatherEffect,
    string WindowVideoQuery,
    string MusicGenre,
    string MusicSearchQuery,
    float MusicVolume,
    string AmbienceType,
    float AmbienceVolume,
    string NoiseType,
    float NoiseVolume,
    int CutoffFrequencyHz,
    string TextureType,
    float TextureVolume,
    int RecommendedMinutes,
    int BreakMinutes,
    string FlowShieldLevel,
    bool IsAiGenerated,
    string? PromptUsed,
    DateTime CreatedAt
);
