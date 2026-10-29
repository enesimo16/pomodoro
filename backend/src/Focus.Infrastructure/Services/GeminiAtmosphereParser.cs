using System.Text.Json;
using System.Text.RegularExpressions;
using Focus.Application.Features.Atmosphere.DTOs;

namespace Focus.Infrastructure.Services;

internal static partial class GeminiAtmosphereParser
{
    private static readonly string[] Aesthetics = ["CyberpunkNeon", "RetroStudio", "RainyLoft", "JapaneseZen", "MidnightMinimal"];
    private static readonly string[] WeatherEffects = ["Rain", "Snow", "Clear", "Thunderstorm", "Fog"];
    private static readonly string[] AmbienceTypes = ["RainOnWindow", "Fireplace", "BusyCafe", "NightForest", "Silent"];
    private static readonly string[] NoiseTypes = ["BrownNoise", "PinkNoise", "WhiteNoise"];
    private static readonly string[] TextureTypes = ["MechanicalKeyboard", "VinylCrackle", "TapeHiss", "PageFlip"];
    private static readonly string[] FlowShieldLevels = ["Standard", "Strict"];

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColorRegex();

    [GeneratedRegex("^[a-z0-9_]{1,100}$")]
    private static partial Regex CatalogIdRegex();

    public static GeneratedAtmosphereDto Parse(JsonElement root)
    {
        var vt = Obj(root, "visualTheme");
        var sm = Obj(root, "soundscapeMixer");
        var l1 = Obj(sm, "layer1Melody");
        var l2 = Obj(sm, "layer2Ambience");
        var l3 = Obj(sm, "layer3Noise");
        var l4 = Obj(sm, "layer4Texture");
        var rs = Obj(root, "recommendedSession");

        return new GeneratedAtmosphereDto(
            AtmosphereId: Guid.NewGuid(),
            ThemeName: Text(root, "themeName", 100, "Ozel AI Temasi"),
            Description: Text(root, "description", 400, "AI tarafindan uretilmis atmosfer."),
            VisualTheme: new VisualThemeDto(
                Choice(vt, "aesthetic", Aesthetics, "RetroStudio"),
                Color(vt, "wallColor", "#1f1d24"),
                Color(vt, "floorColor", "#141318"),
                Color(vt, "accentLightColor", "#ffb86c"),
                CatalogId(vt, "wallPaperId", "wallpaper_brick_white"),
                CatalogId(vt, "floorId", "floor_parquet_oak"),
                Choice(vt, "weatherEffect", WeatherEffects, "Clear"),
                Text(vt, "windowVideoQuery", 80, "lofi rain window")),
            SoundscapeMixer: new SoundscapeMixerPresetDto(
                Text(sm, "presetName", 100, "AI Akustik Denge"),
                new MelodyLayerDto(
                    Text(l1, "genre", 60, "Lo-Fi Chillhop"),
                    Text(l1, "searchQuery", 80, "lofi chillhop"),
                    Volume(l1, "volume", 0.65f),
                    Int(l1, "targetBpm", 40, 200, 80)),
                new AmbienceLayerDto(
                    Choice(l2, "ambienceType", AmbienceTypes, "RainOnWindow"),
                    Volume(l2, "volume", 0.6f)),
                new NoiseLayerDto(
                    Choice(l3, "noiseType", NoiseTypes, "BrownNoise"),
                    Volume(l3, "volume", 0.35f),
                    Int(l3, "cutoffFrequencyHz", 100, 8000, 450)),
                new TextureLayerDto(
                    Choice(l4, "textureType", TextureTypes, "MechanicalKeyboard"),
                    Volume(l4, "volume", 0.3f))),
            RecommendedSession: new RecommendedSessionDto(
                Int(rs, "durationMinutes", 5, 180, 45),
                Int(rs, "breakMinutes", 1, 60, 10),
                Choice(rs, "flowShieldLevel", FlowShieldLevels, "Standard"),
                Int(rs, "targetRounds", 1, 12, 3)),
            CoachMessage: Text(root, "coachMessage", 400, "Masan ve ritmin hazir. Iyi odaklanmalar!"),
            Source: "PromptGenerated",
            CreatedAt: DateTime.UtcNow);
    }

    private static JsonElement Obj(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : default;

    private static string? Raw(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string Text(JsonElement parent, string name, int maxLength, string fallback)
    {
        var clean = PromptSafety.SanitizeInline(Raw(parent, name), maxLength);
        return clean.Length == 0 ? fallback : clean;
    }

    private static string Choice(JsonElement parent, string name, string[] allowed, string fallback)
    {
        var raw = Raw(parent, name)?.Trim();
        return allowed.FirstOrDefault(a => string.Equals(a, raw, StringComparison.OrdinalIgnoreCase)) ?? fallback;
    }

    private static string Color(JsonElement parent, string name, string fallback)
    {
        var raw = Raw(parent, name)?.Trim();
        return raw != null && HexColorRegex().IsMatch(raw) ? raw : fallback;
    }

    private static string CatalogId(JsonElement parent, string name, string fallback)
    {
        var raw = Raw(parent, name)?.Trim();
        return raw != null && CatalogIdRegex().IsMatch(raw) ? raw : fallback;
    }

    private static float Volume(JsonElement parent, string name, float fallback)
    {
        if (parent.ValueKind == JsonValueKind.Object
            && parent.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetDouble(out var number)
            && double.IsFinite(number))
        {
            return (float)Math.Clamp(number, 0.0, 1.0);
        }

        return fallback;
    }

    private static int Int(JsonElement parent, string name, int min, int max, int fallback)
    {
        if (parent.ValueKind == JsonValueKind.Object
            && parent.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetDouble(out var number)
            && double.IsFinite(number))
        {
            return (int)Math.Clamp(Math.Round(number), min, max);
        }

        return fallback;
    }
}
