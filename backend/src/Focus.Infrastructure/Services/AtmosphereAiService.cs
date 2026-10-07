using System.Net.Http.Json;
using System.Text.Json;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Atmosphere.DTOs;
using Focus.Application.Features.Coach.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.Services;

public class AtmosphereAiService : IAtmosphereAiService
{
    private readonly ApplicationDbContext _context;
    private readonly IFocusCoachEngine _coachEngine;
    private readonly IVectorMemoryService _vectorMemoryService;
    private readonly IAnonymizationService _anonymizationService;
    private readonly IWeatherService _weatherService;
    private readonly HttpClient _httpClient;
    private readonly ExternalApiSettings _apiSettings;
    private readonly ILogger<AtmosphereAiService> _logger;

    public AtmosphereAiService(
        ApplicationDbContext context,
        IFocusCoachEngine coachEngine,
        IVectorMemoryService vectorMemoryService,
        IAnonymizationService anonymizationService,
        IWeatherService weatherService,
        HttpClient httpClient,
        IOptions<ExternalApiSettings> apiSettings,
        ILogger<AtmosphereAiService> logger)
    {
        _context = context;
        _coachEngine = coachEngine;
        _vectorMemoryService = vectorMemoryService;
        _anonymizationService = anonymizationService;
        _weatherService = weatherService;
        _httpClient = httpClient;
        _apiSettings = apiSettings.Value;
        _logger = logger;
    }

    public async Task<GeneratedAtmosphereDto> GenerateAtmosphereAsync(
        Guid userId,
        GenerateAtmosphereRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("Kullanici bulunamadi.");
        }

        // 1. Kisisel Hafiza ve Kolektif Bilgelik Sorgulari
        var queryText = !string.IsNullOrWhiteSpace(request.Prompt)
            ? request.Prompt.Trim()
            : $"{request.FocusGoal} {request.MoodTarget} {request.PreferredAmbience} {request.VisualAesthetic}";

        var queryEmbedding = await _vectorMemoryService.GenerateEmbeddingAsync(queryText, cancellationToken);
        var memories = await _vectorMemoryService.QueryHybridMemoriesAsync(userId, queryEmbedding, topK: 3, cancellationToken);

        GeneratedAtmosphereDto? result = null;

        // 2. Gemini API ile Uretim (Anahtar varsa)
        if (!string.IsNullOrWhiteSpace(_apiSettings.GeminiApiKey))
        {
            try
            {
                result = await GenerateWithGeminiAsync(user.DisplayName, request, memories, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini atmosfer uretiminde hata alindi, deterministik kural motoruna geciliyor.");
            }
        }

        // 3. Fallback: Deterministik Kural Motoruyla Zengin Atmosfer Sentezi
        result ??= SynthesizeDeterministicAtmosphere(user.DisplayName, request, memories, "PromptGenerated");

        // 4. Kisisel Tercih Tespiti ve Kisisel Hafiza Kaydi (Katman 1)
        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            var lower = request.Prompt.ToLowerInvariant();
            if (lower.Contains("severim") || lower.Contains("tercih") || lower.Contains("hep") || lower.Contains("benim tarzım"))
            {
                var memoryContent = $"Kullanicinin atmosfer tercihi: {request.Prompt.Trim()}";
                var memEmbedding = await _vectorMemoryService.GenerateEmbeddingAsync(memoryContent, cancellationToken);
                await _vectorMemoryService.AddPrivateMemoryAsync(
                    userId,
                    MemoryCategory.AtmospherePreference,
                    memoryContent,
                    memEmbedding,
                    importance: 4,
                    cancellationToken: cancellationToken);
            }
        }

        return result;
    }

    public async Task<GeneratedAtmosphereDto> GenerateAutoAtmosphereAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("Kullanici bulunamadi.");
        }

        var today = DateTime.UtcNow.Date;
        var todaySessions = await _context.FocusSessions
            .Where(s => s.UserId == userId && s.StartedAt >= today)
            .ToListAsync(cancellationToken);

        // 1. Gunluk Yorgunluk Skoru ve Sirkadiyen Dilim Hesabi
        var fatigueScore = _coachEngine.CalculateDailyFatigueScore(todaySessions);
        var currentHour = DateTime.UtcNow.AddHours(3).Hour; // Yerel saat (UTC+3)

        var circadianSlot = currentHour switch
        {
            >= 6 and < 11 => "Morning",
            >= 11 and < 17 => "Afternoon",
            >= 17 and < 22 => "Evening",
            _ => "LateNight"
        };

        // 2. Son Seans Yansimalari ve Ruh Hali Analizi
        var recentReflections = await _context.SessionReflections
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var hadRecentDistractions = recentReflections.Count != 0 && recentReflections.Average(r => r.FocusQuality) < 3.0;

        // 3. Anlik Hava Durumu
        var weather = await _weatherService.GetCurrentWeatherAsync(cancellationToken: cancellationToken);
        var weatherCondition = weather?.WeatherCondition ?? "Clear";

        // 4. Kolektif Bilgelik Sorgusu (Katman 2)
        var contextQuery = $"{circadianSlot} yorgunluk:{fatigueScore:F0} hava:{weatherCondition}";
        var contextEmbedding = await _vectorMemoryService.GenerateEmbeddingAsync(contextQuery, cancellationToken);
        var memories = await _vectorMemoryService.QueryHybridMemoriesAsync(userId, contextEmbedding, topK: 3, cancellationToken);

        // 5. Analitik Tabanli Otomatik Sentez
        var focusGoal = (fatigueScore >= 60 || hadRecentDistractions)
            ? "Meditation_Relax"
            : (circadianSlot == "LateNight" ? "DeepWork_Coding" : "Reading_Study");

        var autoRequest = new GenerateAtmosphereRequestDto(
            Prompt: null,
            FocusGoal: focusGoal,
            MoodTarget: fatigueScore >= 60 ? "CalmZen" : (circadianSlot == "Morning" ? "EnergeticFlow" : "MidnightFocus"),
            PreferredAmbience: weatherCondition.Contains("Rain", StringComparison.OrdinalIgnoreCase) ? "RainWindow" : "CracklingFireplace",
            VisualAesthetic: circadianSlot == "LateNight" ? "CyberpunkNeon" : (circadianSlot == "Morning" ? "JapaneseZen" : "RetroStudio"),
            TimeOfDay: circadianSlot);

        var generated = SynthesizeDeterministicAtmosphere(user.DisplayName, autoRequest, memories, "AutoRecommended");

        // Yorgunluk yuksekse seans suresini otomatik kisalt
        if (fatigueScore >= 70)
        {
            generated = generated with
            {
                RecommendedSession = new RecommendedSessionDto(20, 10, "Standard", 2),
                CoachMessage = $"{user.DisplayName}, gunluk yorgunluk skorun {fatigueScore:F0}/100 seviyesinde. Zihnine yuklenmeden 20 dakikalik hafif bir toparlanma seansi oneriyorum."
            };
        }

        return generated;
    }

    public async Task<bool> ContributeSessionInsightAsync(
        Guid userId,
        Guid? sessionId = null,
        int? plannedDurationMinutes = null,
        int? focusQuality = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var userName = user?.DisplayName;

        int duration = plannedDurationMinutes ?? 45;
        int quality = focusQuality ?? 5;
        string? rawNote = notes;
        int hour = DateTime.UtcNow.AddHours(3).Hour;

        if (sessionId.HasValue && sessionId.Value != Guid.Empty)
        {
            var session = await _context.FocusSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId.Value && s.UserId == userId, cancellationToken);

            if (session != null)
            {
                if (session.Status != SessionStatus.Completed)
                {
                    return false;
                }
                duration = session.PlannedMinutes;
                hour = session.StartedAt.AddHours(3).Hour;

                var reflection = await _context.SessionReflections
                    .FirstOrDefaultAsync(r => r.SessionId == sessionId.Value, cancellationToken);

                if (reflection != null)
                {
                    quality = reflection.FocusQuality;
                    rawNote = reflection.DistractionNote ?? rawNote;
                }
            }
        }

        // Yalnizca kaliteli ve basarili seanslar kolektif AI'ye ogretilir (FocusQuality >= 4)
        if (quality < 4)
        {
            return false;
        }

        // PII ve Kisisel Verileri Guvenle Temizle (Katman 2 Kolektif Anonimlestirme)
        var sanitizedNote = !string.IsNullOrWhiteSpace(rawNote)
            ? _anonymizationService.AnonymizeAndGeneralize(rawNote, userName)
            : "kesintisiz derin calisma saglandi";

        var timeOfDay = hour switch
        {
            >= 6 and < 11 => "Sabah",
            >= 11 and < 17 => "Ogleden Sonra",
            >= 17 and < 22 => "Aksam",
            _ => "Gece Yarisi"
        };

        var collectiveInsight = $"[Kolektif Bilgelik] {timeOfDay} diliminde {duration} dakikalik seansta yuksek odak basarisi: {sanitizedNote}.";

        var embedding = await _vectorMemoryService.GenerateEmbeddingAsync(collectiveInsight, cancellationToken);

        var globalMemory = AgentMemory.CreateGlobal(
            MemoryCategory.CollectiveWisdom,
            collectiveInsight,
            embedding,
            importance: 4);

        _context.AgentMemories.Add(globalMemory);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Kolektif AI bellegine anonim seans icgorusu eklendi: {Insight}", collectiveInsight);
        return true;
    }

    private async Task<GeneratedAtmosphereDto> GenerateWithGeminiAsync(
        string displayName,
        GenerateAtmosphereRequestDto request,
        List<MemoryInsightDto> memories,
        CancellationToken cancellationToken)
    {
        var memoryContext = string.Join("\n", memories.Select(m => $"- {(m.IsGlobal ? "[Kolektif]" : "[Kisisel]")}: {m.Content}"));
        var userPrompt = !string.IsNullOrWhiteSpace(request.Prompt)
            ? request.Prompt.Trim()
            : $"Odak: {request.FocusGoal}, Ruh Hali: {request.MoodTarget}, Ambiyans: {request.PreferredAmbience}, Estetik: {request.VisualAesthetic}, Zaman: {request.TimeOfDay}";

        var systemPrompt = $$"""
            Sen Habbo tarzi retro piksel ev ekosistemi ve 4 kanalli ses mikseri icin yapay zeka atmosfer tasarim motorusun.
            Kullanici isteklerini analiz ederek JSON formatinda tam bir oda temasi ve ses mikseri preseti uret.
            
            Kullanici: {{displayName}}
            Mevcut Hafiza ve Kolektif Bilgiler:
            {{memoryContext}}

            Yalnizca su JSON semasinda cevap ver (markdown code block olmadan saf JSON):
            {
              "themeName": "...",
              "description": "...",
              "visualTheme": {
                "aesthetic": "CyberpunkNeon|RetroStudio|RainyLoft|JapaneseZen|MidnightMinimal",
                "wallColor": "#hex",
                "floorColor": "#hex",
                "accentLightColor": "#hex",
                "wallPaperId": "wallpaper_brick_white",
                "floorId": "floor_parquet_oak",
                "weatherEffect": "Rain|Snow|Clear|Thunderstorm|Fog",
                "windowVideoQuery": "..."
              },
              "soundscapeMixer": {
                "presetName": "...",
                "layer1Melody": { "genre": "...", "searchQuery": "...", "volume": 0.65, "targetBpm": 80 },
                "layer2Ambience": { "ambienceType": "RainOnWindow|Fireplace|BusyCafe|NightForest|Silent", "volume": 0.60 },
                "layer3Noise": { "noiseType": "BrownNoise|PinkNoise|WhiteNoise", "volume": 0.35, "cutoffFrequencyHz": 450 },
                "layer4Texture": { "textureType": "MechanicalKeyboard|VinylCrackle|TapeHiss|PageFlip", "volume": 0.30 }
              },
              "recommendedSession": {
                "durationMinutes": 45,
                "breakMinutes": 10,
                "flowShieldLevel": "Standard|Strict",
                "targetRounds": 3
              },
              "coachMessage": "..."
            }
            """;

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiSettings.GeminiApiKey}";
        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userPrompt } } }
            },
            generationConfig = new
            {
                maxOutputTokens = 800,
                temperature = 0.7,
                response_mime_type = "application/json"
            }
        };

        var res = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
        if (res.IsSuccessStatusCode)
        {
            var root = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var text = root.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                var je = doc.RootElement;

                var vt = je.GetProperty("visualTheme");
                var sm = je.GetProperty("soundscapeMixer");
                var l1 = sm.GetProperty("layer1Melody");
                var l2 = sm.GetProperty("layer2Ambience");
                var l3 = sm.GetProperty("layer3Noise");
                var l4 = sm.GetProperty("layer4Texture");
                var rs = je.GetProperty("recommendedSession");

                return new GeneratedAtmosphereDto(
                    AtmosphereId: Guid.NewGuid(),
                    ThemeName: je.GetProperty("themeName").GetString() ?? "Ozel AI Temasi",
                    Description: je.GetProperty("description").GetString() ?? "AI tarafindan uretilmis atmosfer.",
                    VisualTheme: new VisualThemeDto(
                        vt.GetProperty("aesthetic").GetString() ?? "RetroStudio",
                        vt.GetProperty("wallColor").GetString() ?? "#1f1d24",
                        vt.GetProperty("floorColor").GetString() ?? "#141318",
                        vt.GetProperty("accentLightColor").GetString() ?? "#ffb86c",
                        vt.GetProperty("wallPaperId").GetString() ?? "wallpaper_brick_white",
                        vt.GetProperty("floorId").GetString() ?? "floor_parquet_oak",
                        vt.GetProperty("weatherEffect").GetString() ?? "Rain",
                        vt.GetProperty("windowVideoQuery").GetString() ?? "lofi rain window"
                    ),
                    SoundscapeMixer: new SoundscapeMixerPresetDto(
                        sm.GetProperty("presetName").GetString() ?? "AI Akustik Denge",
                        new MelodyLayerDto(
                            l1.GetProperty("genre").GetString() ?? "Lo-Fi Chillhop",
                            l1.GetProperty("searchQuery").GetString() ?? "lofi chillhop",
                            (float)l1.GetProperty("volume").GetDouble(),
                            l1.GetProperty("targetBpm").GetInt32()
                        ),
                        new AmbienceLayerDto(
                            l2.GetProperty("ambienceType").GetString() ?? "RainOnWindow",
                            (float)l2.GetProperty("volume").GetDouble()
                        ),
                        new NoiseLayerDto(
                            l3.GetProperty("noiseType").GetString() ?? "BrownNoise",
                            (float)l3.GetProperty("volume").GetDouble(),
                            l3.GetProperty("cutoffFrequencyHz").GetInt32()
                        ),
                        new TextureLayerDto(
                            l4.GetProperty("textureType").GetString() ?? "MechanicalKeyboard",
                            (float)l4.GetProperty("volume").GetDouble()
                        )
                    ),
                    RecommendedSession: new RecommendedSessionDto(
                        rs.GetProperty("durationMinutes").GetInt32(),
                        rs.GetProperty("breakMinutes").GetInt32(),
                        rs.GetProperty("flowShieldLevel").GetString() ?? "Standard",
                        rs.GetProperty("targetRounds").GetInt32()
                    ),
                    CoachMessage: je.GetProperty("coachMessage").GetString() ?? "Masan ve ritmin hazir. Iyi odaklanmalar!",
                    Source: "PromptGenerated",
                    CreatedAt: DateTime.UtcNow
                );
            }
        }

        throw new InvalidOperationException("Gemini bos yanit dondurdu.");
    }

    private static GeneratedAtmosphereDto SynthesizeDeterministicAtmosphere(
        string displayName,
        GenerateAtmosphereRequestDto request,
        List<MemoryInsightDto> memories,
        string source)
    {
        var promptLower = (request.Prompt ?? string.Empty).ToLowerInvariant();
        var isCyberpunk = promptLower.Contains("cyber") || promptLower.Contains("neon") || request.VisualAesthetic == "CyberpunkNeon";
        var isZen = promptLower.Contains("zen") || promptLower.Contains("japon") || promptLower.Contains("sakin") || request.VisualAesthetic == "JapaneseZen";
        var isLoft = promptLower.Contains("loft") || promptLower.Contains("çatı") || promptLower.Contains("cati") || request.VisualAesthetic == "RainyLoft";
        var isMinimal = promptLower.Contains("minimal") || promptLower.Contains("oled") || promptLower.Contains("karanlık") || request.VisualAesthetic == "MidnightMinimal";

        // 1. Görsel Palet Seçimi
        string aesthetic;
        string themeName;
        string description;
        string wallColor;
        string floorColor;
        string accentLightColor;
        string wallpaperId;
        string floorId;
        string weatherEffect;
        string videoQuery;

        if (isCyberpunk)
        {
            aesthetic = "CyberpunkNeon";
            themeName = "Siber Gece & Neon Yağmur";
            description = "Camgöbeği neon ışıklar, ıslak sokak yansımaları ve derin kodlama konsantrasyonu.";
            wallColor = "#161224";
            floorColor = "#0d0a17";
            accentLightColor = "#00f0ff";
            wallpaperId = "wallpaper_slate_dark";
            floorId = "floor_parquet_oak";
            weatherEffect = "Rain";
            videoQuery = "cyberpunk rain night neon";
        }
        else if (isZen)
        {
            aesthetic = "JapaneseZen";
            themeName = "Kyoto Zen Bahçesi & Bambu";
            description = "Tatami zemin, yumuşak sabah sisi, yeşil çay ve zihni durultan dinginlik.";
            wallColor = "#26231e";
            floorColor = "#1a1814";
            accentLightColor = "#e6d5ac";
            wallpaperId = "wallpaper_wood_cozy";
            floorId = "floor_parquet_oak";
            weatherEffect = "Clear";
            videoQuery = "zen garden bamboo forest";
        }
        else if (isLoft)
        {
            aesthetic = "RainyLoft";
            themeName = "Yağmurlu Çatı Katı & Lo-Fi Kütüphane";
            description = "Eğimli çatı camından süzülen yağmur damlaları ve sıcak sarı lamba ışığı.";
            wallColor = "#241f1c";
            floorColor = "#171412";
            accentLightColor = "#ffb86c";
            wallpaperId = "wallpaper_brick_loft";
            floorId = "floor_parquet_oak";
            weatherEffect = "Rain";
            videoQuery = "rain window coffee cozy loft";
        }
        else if (isMinimal)
        {
            aesthetic = "MidnightMinimal";
            themeName = "Gece Kuşu Zen OLED";
            description = "Yalnızca sen ve masan. Dikkat dağıtıcı tüm mobilyalar sessizliğe büründü.";
            wallColor = "#121214";
            floorColor = "#0a0a0c";
            accentLightColor = "#ffffff";
            wallpaperId = "wallpaper_slate_dark";
            floorId = "floor_parquet_oak";
            weatherEffect = "Clear";
            videoQuery = "night sky stars minimal";
        }
        else
        {
            aesthetic = "RetroStudio";
            themeName = "Retro Ahşap Stüdyo & Plak Çalar";
            description = "Sıcak tuğla duvar, vintage plak çalar ve zamansız çalışma odası ambiyansı.";
            wallColor = "#2b2520";
            floorColor = "#1c1815";
            accentLightColor = "#f4a261";
            wallpaperId = "wallpaper_brick_white";
            floorId = "floor_parquet_oak";
            weatherEffect = "Clear";
            videoQuery = "lofi study room cozy";
        }

        // 2. 4-Kanallı Akustik Mikser Seçimi
        var wantsRain = promptLower.Contains("yağmur") || promptLower.Contains("rain") || request.PreferredAmbience == "RainWindow";
        var wantsFireplace = promptLower.Contains("şömine") || promptLower.Contains("somine") || promptLower.Contains("ateş") || request.PreferredAmbience == "CracklingFireplace";
        var wantsCafe = promptLower.Contains("kafe") || promptLower.Contains("cafe") || request.PreferredAmbience == "BusyCafe";

        var ambienceType = wantsRain ? "RainOnWindow" : (wantsFireplace ? "Fireplace" : (wantsCafe ? "BusyCafe" : "NightForest"));
        var ambienceVolume = wantsRain ? 0.65f : 0.50f;

        var noiseType = isCyberpunk || promptLower.Contains("kod") || request.FocusGoal == "DeepWork_Coding"
            ? "BrownNoise"
            : (isZen ? "PinkNoise" : "WhiteNoise");
        var noiseCutoff = noiseType == "BrownNoise" ? 420 : 800;

        var melodyGenre = isCyberpunk ? "Synthwave Lofi" : (isZen ? "Ambient Piano Koto" : "Lo-Fi Chillhop Piano");
        var melodySearch = isCyberpunk ? "synthwave lofi night" : (isZen ? "ambient acoustic zen" : "lofi chillhop beats");
        var targetBpm = isCyberpunk ? 82 : (isZen ? 68 : 76);

        var textureType = promptLower.Contains("klavye") || isCyberpunk
            ? "MechanicalKeyboard"
            : (isLoft ? "VinylCrackle" : "TapeHiss");

        // 3. Seans Önerisi
        var isDeepWork = isCyberpunk || promptLower.Contains("kod") || promptLower.Contains("code") || request.FocusGoal == "DeepWork_Coding";
        var durationMinutes = isDeepWork ? 50 : (request.FocusGoal == "Urgent_Sprint" ? 25 : 35);
        var breakMinutes = durationMinutes >= 45 ? 10 : 5;
        var shield = durationMinutes >= 45 ? "Strict" : "Standard";

        // 4. Bilge Koç Mesajı
        var primaryMemory = memories.FirstOrDefault()?.Content;
        var memoryHint = !string.IsNullOrWhiteSpace(primaryMemory) ? $" {primaryMemory}" : string.Empty;

        var coachQuote = isCyberpunk
            ? $"Siber frekanslar hazır {displayName}. Zihnini derin akışa bırak, ekrandaki her satır senin ritminle akacak.{memoryHint}"
            : (isZen
                ? $"Zihnindeki gürültü dindi {displayName}. Bambu yapraklarının sükunetiyle bu seansı sakince tamamlayalım.{memoryHint}"
                : $"Masa lamban ve plak çaların senin için ayarlandı {displayName}. 4 kanallı akustik dengede harika bir seans seni bekliyor.{memoryHint}");

        return new GeneratedAtmosphereDto(
            AtmosphereId: Guid.NewGuid(),
            ThemeName: themeName,
            Description: description,
            VisualTheme: new VisualThemeDto(
                aesthetic,
                wallColor,
                floorColor,
                accentLightColor,
                wallpaperId,
                floorId,
                weatherEffect,
                videoQuery
            ),
            SoundscapeMixer: new SoundscapeMixerPresetDto(
                $"{themeName} Mikseri",
                new MelodyLayerDto(melodyGenre, melodySearch, 0.65f, targetBpm),
                new AmbienceLayerDto(ambienceType, ambienceVolume),
                new NoiseLayerDto(noiseType, 0.35f, noiseCutoff),
                new TextureLayerDto(textureType, 0.30f)
            ),
            RecommendedSession: new RecommendedSessionDto(durationMinutes, breakMinutes, shield, 4),
            CoachMessage: coachQuote,
            Source: source,
            CreatedAt: DateTime.UtcNow
        );
    }
}
