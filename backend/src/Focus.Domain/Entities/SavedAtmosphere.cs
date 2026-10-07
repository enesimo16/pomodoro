namespace Focus.Domain.Entities;

using Focus.Domain.Common;

public class SavedAtmosphere : BaseEntity<Guid>, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Aesthetic { get; private set; } = "RetroStudio";
    public string WallColor { get; private set; } = "#2b2520";
    public string FloorColor { get; private set; } = "#1c1815";
    public string AccentLightColor { get; private set; } = "#ffb86c";
    public string? WallPaperId { get; private set; }
    public string? FloorId { get; private set; }
    public string WeatherEffect { get; private set; } = "Clear";
    public string WindowVideoQuery { get; private set; } = "lofi room window";

    // 4-Kanal Akustik Ayarlari
    public string MusicGenre { get; private set; } = "Lo-Fi Chillhop";
    public string MusicSearchQuery { get; private set; } = "lofi chillhop";
    public float MusicVolume { get; private set; } = 0.6f;
    public string AmbienceType { get; private set; } = "RainOnWindow";
    public float AmbienceVolume { get; private set; } = 0.5f;
    public string NoiseType { get; private set; } = "BrownNoise";
    public float NoiseVolume { get; private set; } = 0.3f;
    public int CutoffFrequencyHz { get; private set; } = 450;
    public string TextureType { get; private set; } = "MechanicalKeyboard";
    public float TextureVolume { get; private set; } = 0.3f;

    // Seans Onerileri
    public int RecommendedMinutes { get; private set; } = 25;
    public int BreakMinutes { get; private set; } = 5;
    public string FlowShieldLevel { get; private set; } = "Standard";

    // AI Meta Verileri
    public bool IsAiGenerated { get; private set; }
    public string? PromptUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private SavedAtmosphere() { }

    public static SavedAtmosphere Create(
        Guid userId,
        string name,
        string description,
        string aesthetic,
        string wallColor,
        string floorColor,
        string accentLightColor,
        string? wallPaperId,
        string? floorId,
        string weatherEffect,
        string windowVideoQuery,
        string musicGenre,
        string musicSearchQuery,
        float musicVolume,
        string ambienceType,
        float ambienceVolume,
        string noiseType,
        float noiseVolume,
        int cutoffFrequencyHz,
        string textureType,
        float textureVolume,
        int recommendedMinutes,
        int breakMinutes,
        string flowShieldLevel,
        bool isAiGenerated,
        string? promptUsed)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Atmosfer adi bos olamaz.", nameof(name));

        return new SavedAtmosphere
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Aesthetic = string.IsNullOrWhiteSpace(aesthetic) ? "RetroStudio" : aesthetic.Trim(),
            WallColor = string.IsNullOrWhiteSpace(wallColor) ? "#2b2520" : wallColor.Trim(),
            FloorColor = string.IsNullOrWhiteSpace(floorColor) ? "#1c1815" : floorColor.Trim(),
            AccentLightColor = string.IsNullOrWhiteSpace(accentLightColor) ? "#ffb86c" : accentLightColor.Trim(),
            WallPaperId = wallPaperId,
            FloorId = floorId,
            WeatherEffect = string.IsNullOrWhiteSpace(weatherEffect) ? "Clear" : weatherEffect.Trim(),
            WindowVideoQuery = string.IsNullOrWhiteSpace(windowVideoQuery) ? "lofi room window" : windowVideoQuery.Trim(),
            MusicGenre = string.IsNullOrWhiteSpace(musicGenre) ? "Lo-Fi Chillhop" : musicGenre.Trim(),
            MusicSearchQuery = string.IsNullOrWhiteSpace(musicSearchQuery) ? "lofi chillhop" : musicSearchQuery.Trim(),
            MusicVolume = Math.Clamp(musicVolume, 0f, 1f),
            AmbienceType = string.IsNullOrWhiteSpace(ambienceType) ? "RainOnWindow" : ambienceType.Trim(),
            AmbienceVolume = Math.Clamp(ambienceVolume, 0f, 1f),
            NoiseType = string.IsNullOrWhiteSpace(noiseType) ? "BrownNoise" : noiseType.Trim(),
            NoiseVolume = Math.Clamp(noiseVolume, 0f, 1f),
            CutoffFrequencyHz = Math.Clamp(cutoffFrequencyHz, 50, 20000),
            TextureType = string.IsNullOrWhiteSpace(textureType) ? "MechanicalKeyboard" : textureType.Trim(),
            TextureVolume = Math.Clamp(textureVolume, 0f, 1f),
            RecommendedMinutes = Math.Clamp(recommendedMinutes, 5, 180),
            BreakMinutes = Math.Clamp(breakMinutes, 1, 60),
            FlowShieldLevel = string.IsNullOrWhiteSpace(flowShieldLevel) ? "Standard" : flowShieldLevel.Trim(),
            IsAiGenerated = isAiGenerated,
            PromptUsed = promptUsed?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
