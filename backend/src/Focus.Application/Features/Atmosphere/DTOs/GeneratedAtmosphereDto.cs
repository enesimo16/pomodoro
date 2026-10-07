namespace Focus.Application.Features.Atmosphere.DTOs;

public record VisualThemeDto
{
    public string Aesthetic { get; init; } = string.Empty;
    public string WallColor { get; init; } = "#161224";
    public string FloorColor { get; init; } = "#0d0a17";
    public string AccentLightColor { get; init; } = "#00f0ff";
    public string? WallPaperId { get; init; }
    public string? FloorId { get; init; }
    public string WeatherEffect { get; init; } = "Clear";
    public string WindowVideoQuery { get; init; } = "cozy study";

    public VisualThemeDto() { }
    public VisualThemeDto(
        string aesthetic,
        string wallColor,
        string floorColor,
        string accentLightColor,
        string? wallPaperId,
        string? floorId,
        string weatherEffect,
        string windowVideoQuery)
    {
        Aesthetic = aesthetic;
        WallColor = wallColor;
        FloorColor = floorColor;
        AccentLightColor = accentLightColor;
        WallPaperId = wallPaperId;
        FloorId = floorId;
        WeatherEffect = weatherEffect;
        WindowVideoQuery = windowVideoQuery;
    }
}

public record MelodyLayerDto
{
    public string Genre { get; init; } = "Lo-Fi Beats";
    public string SearchQuery { get; init; } = "lofi study";
    public float Volume { get; init; } = 0.65f;
    public int TargetBpm { get; init; } = 80;

    public MelodyLayerDto() { }
    public MelodyLayerDto(string genre, string searchQuery, float volume, int targetBpm)
    {
        Genre = genre;
        SearchQuery = searchQuery;
        Volume = volume;
        TargetBpm = targetBpm;
    }
}

public record AmbienceLayerDto
{
    public string AmbienceType { get; init; } = "Rainstorm";
    public float Volume { get; init; } = 0.5f;

    public AmbienceLayerDto() { }
    public AmbienceLayerDto(string ambienceType, float volume)
    {
        AmbienceType = ambienceType;
        Volume = volume;
    }
}

public record NoiseLayerDto
{
    public string NoiseType { get; init; } = "BrownNoise";
    public float Volume { get; init; } = 0.35f;
    public int CutoffFrequencyHz { get; init; } = 400;

    public NoiseLayerDto() { }
    public NoiseLayerDto(string noiseType, float volume, int cutoffFrequencyHz)
    {
        NoiseType = noiseType;
        Volume = volume;
        CutoffFrequencyHz = cutoffFrequencyHz;
    }
}

public record TextureLayerDto
{
    public string TextureType { get; init; } = "VinylCrackle";
    public float Volume { get; init; } = 0.25f;

    public TextureLayerDto() { }
    public TextureLayerDto(string textureType, float volume)
    {
        TextureType = textureType;
        Volume = volume;
    }
}

public record SoundscapeMixerPresetDto
{
    public string PresetName { get; init; } = string.Empty;
    public MelodyLayerDto Layer1Melody { get; init; } = new();
    public AmbienceLayerDto Layer2Ambience { get; init; } = new();
    public NoiseLayerDto Layer3Noise { get; init; } = new();
    public TextureLayerDto Layer4Texture { get; init; } = new();

    public SoundscapeMixerPresetDto() { }
    public SoundscapeMixerPresetDto(
        string presetName,
        MelodyLayerDto layer1Melody,
        AmbienceLayerDto layer2Ambience,
        NoiseLayerDto layer3Noise,
        TextureLayerDto layer4Texture)
    {
        PresetName = presetName;
        Layer1Melody = layer1Melody;
        Layer2Ambience = layer2Ambience;
        Layer3Noise = layer3Noise;
        Layer4Texture = layer4Texture;
    }
}

public record RecommendedSessionDto
{
    public int DurationMinutes { get; init; } = 45;
    public int BreakMinutes { get; init; } = 5;
    public string FlowShieldLevel { get; init; } = "Standard";
    public int TargetRounds { get; init; } = 4;

    public RecommendedSessionDto() { }
    public RecommendedSessionDto(int durationMinutes, int breakMinutes, string flowShieldLevel, int targetRounds)
    {
        DurationMinutes = durationMinutes;
        BreakMinutes = breakMinutes;
        FlowShieldLevel = flowShieldLevel;
        TargetRounds = targetRounds;
    }
}

public record GeneratedAtmosphereDto
{
    public Guid? AtmosphereId { get; init; }
    public string ThemeName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public VisualThemeDto VisualTheme { get; init; } = new();
    public SoundscapeMixerPresetDto SoundscapeMixer { get; init; } = new();
    public RecommendedSessionDto RecommendedSession { get; init; } = new();
    public string CoachMessage { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public GeneratedAtmosphereDto() { }
    public GeneratedAtmosphereDto(
        Guid? AtmosphereId,
        string ThemeName,
        string Description,
        VisualThemeDto VisualTheme,
        SoundscapeMixerPresetDto SoundscapeMixer,
        RecommendedSessionDto RecommendedSession,
        string CoachMessage,
        string Source,
        DateTime CreatedAt)
    {
        this.AtmosphereId = AtmosphereId;
        this.ThemeName = ThemeName;
        this.Description = Description;
        this.VisualTheme = VisualTheme;
        this.SoundscapeMixer = SoundscapeMixer;
        this.RecommendedSession = RecommendedSession;
        this.CoachMessage = CoachMessage;
        this.Source = Source;
        this.CreatedAt = CreatedAt;
    }
}
