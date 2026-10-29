using System.Net.Http.Json;
using System.Text.Json;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.Services;

public class CoachChatService : ICoachChatService
{
    private readonly ApplicationDbContext _context;
    private readonly IFocusCoachEngine _coachEngine;
    private readonly IVectorMemoryService _vectorMemoryService;
    private readonly IAiUsageService _aiUsageService;
    private const int ChatMaxOutputTokens = 150;

    private readonly HttpClient _httpClient;
    private readonly AiSettings _aiSettings;
    private readonly ILogger<CoachChatService> _logger;

    public CoachChatService(
        ApplicationDbContext context,
        IFocusCoachEngine coachEngine,
        IVectorMemoryService vectorMemoryService,
        IAiUsageService aiUsageService,
        HttpClient httpClient,
        IOptions<AiSettings> aiSettings,
        ILogger<CoachChatService> logger)
    {
        _context = context;
        _coachEngine = coachEngine;
        _vectorMemoryService = vectorMemoryService;
        _aiUsageService = aiUsageService;
        _httpClient = httpClient;
        _aiSettings = aiSettings.Value;
        _logger = logger;
    }

    public async Task<CoachChatResponseDto> GenerateCoachResponseAsync(Guid userId, string userMessage, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("Kullanici bulunamadi.");
        }

        var sinceDate = DateTime.UtcNow.AddDays(-14);

        var recentSessions = await _context.FocusSessions
            .Where(s => s.UserId == userId && s.StartedAt >= sinceDate)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        // Uzun suredir ara veren kullanicinin son seansi pencere disinda kalsa da yokluk tespiti icin eklenir
        if (recentSessions.Count == 0)
        {
            var lastSession = await _context.FocusSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastSession != null)
            {
                recentSessions.Add(lastSession);
            }
        }

        var recentCheckIns = await _context.SessionCheckIns
            .Where(c => c.UserId == userId && c.CreatedAt >= sinceDate)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var recentReflections = await _context.SessionReflections
            .Where(r => r.UserId == userId && r.CreatedAt >= sinceDate)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var streak = await _context.UserStreaks
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        // 1. Kural Motoru Anomalilerini Degerlendir
        var anomaly = _coachEngine.EvaluateAnomalies(user, recentSessions, recentCheckIns, recentReflections, streak);

        // 2. Mesaji Vektorlestir ve Hibrit Hafizayi Sorgula
        var messageEmbedding = await _vectorMemoryService.GenerateEmbeddingAsync(userMessage, cancellationToken);
        var memories = await _vectorMemoryService.QueryHybridMemoriesAsync(userId, messageEmbedding, topK: 3, cancellationToken);

        // 3. Yanit Uret (Gemini veya Kural Tabanli Sentez)
        string reply;

        if (_aiSettings.IsGeminiConfigured)
        {
            reply = await GenerateGeminiResponseAsync(userId, user.DisplayName, userMessage, anomaly, memories, cancellationToken);
        }
        else
        {
            reply = GenerateDeterministicCoachResponse(user.DisplayName, userMessage, anomaly, memories);
        }

        // 4. Eger kullanici bir aliskanlik/tercih paylastiysa yeni hafiza uret
        var generatedNewMemory = false;
        var lowerMsg = userMessage.ToLowerInvariant();
        var agentDisabled = await _context.UserPreferences
            .AnyAsync(p => p.UserId == userId && !p.AgentEnabled, cancellationToken);
        if (!agentDisabled && (lowerMsg.Contains("dakika") || lowerMsg.Contains("severim") || lowerMsg.Contains("muzik") || lowerMsg.Contains("gece") || lowerMsg.Contains("sabah")))
        {
            var newMemoryContent = $"Kullanici tercihi: {userMessage.Trim()}";
            var newEmbedding = await _vectorMemoryService.GenerateEmbeddingAsync(newMemoryContent, cancellationToken);
            await _vectorMemoryService.AddPrivateMemoryAsync(
                userId,
                MemoryCategory.PersonalHabit,
                newMemoryContent,
                newEmbedding,
                importance: 4,
                cancellationToken: cancellationToken);

            generatedNewMemory = true;
        }

        return new CoachChatResponseDto(
            Reply: reply,
            DetectedAnomaly: anomaly.AnomalyType,
            DailyFatigueScore: anomaly.DailyFatigueScore,
            UsedMemories: memories,
            GeneratedNewMemory: generatedNewMemory);
    }

    private async Task<string> GenerateGeminiResponseAsync(
        Guid userId,
        string displayName,
        string userMessage,
        CoachAnomalyStatusDto anomaly,
        List<MemoryInsightDto> memories,
        CancellationToken cancellationToken)
    {
        try
        {
            var systemPrompt = $"""
                Sen kullanicinin calisma masasindaki retro terminalde canlanan bilge, samimi ve kisa konusan kisisel odaklanma kocusun.
                Tıbbi teshis koymazsin. Kullaniciya emir vermezsin veya 'calismayi birak/azalt' demezsin.
                Amacin kullaniciyi sakinlestirmek, aktif toparlanmayi ve derin odagi desteklemektir.
                En fazla 2-3 kisa cumle ile cevap ver.
                {PromptSafety.MemoryBlockInstruction}
                """;

            var userContent = $"""
                Kullanici Adi: {PromptSafety.SanitizeInline(displayName, PromptSafety.MaxDisplayNameLength)}
                Anomali Durumu: {anomaly.AnomalyType} (Yorgunluk Skoru: {anomaly.DailyFatigueScore}/100, Haftalik Odak: {anomaly.WeeklyFocusMinutes} dk)
                Hafiza Bilgileri:
                {PromptSafety.BuildMemoryBlock(memories)}

                Kullanici Mesaji:
                {userMessage}
                """;

            await _aiUsageService.ConsumeOrThrowAsync(
                userId,
                PromptSafety.EstimateTokens(systemPrompt, userContent) + ChatMaxOutputTokens,
                cancellationToken);

            var payload = new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = userContent } } }
                },
                generationConfig = new
                {
                    maxOutputTokens = ChatMaxOutputTokens,
                    temperature = 0.7
                }
            };

            using var res = await _httpClient.PostAsJsonAsync($"models/{_aiSettings.Gemini.ChatModelId}:generateContent", payload, cancellationToken);
            using var json = await GeminiResponse.ReadAsync(res, _logger, "coach-chat", cancellationToken);
            var text = json == null ? null : GeminiResponse.ExtractText(json.RootElement);

            if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Gemini koc yaniti alinamadi, kural tabanli yanita geciliyor.");
        }

        return GenerateDeterministicCoachResponse(displayName, userMessage, anomaly, memories);
    }

    private static string GenerateDeterministicCoachResponse(
        string displayName,
        string userMessage,
        CoachAnomalyStatusDto anomaly,
        List<MemoryInsightDto> memories)
    {
        var primaryMemory = memories.FirstOrDefault()?.Content ?? "Dengeli bloklar zihinsel yorgunlugu azaltir.";

        return anomaly.AnomalyType switch
        {
            AnomalyType.AbsenceReturn =>
                $"Tekrar hos geldin {displayName}. Masan ve ritmin seni bekliyordu. Hafizami kontrol ettigimde, pası atmak icin 15-20 dakikalik hafif bir seansla baslamak harika bir giris olacaktir.",

            AnomalyType.WeeklyExcessiveLoad =>
                $"Bu hafta {anomaly.WeeklyFocusMinutes / 60} saatlik muazzam bir odak emegi verdin {displayName}. Zihin yeni bilgileri dinlenirken ve sosyallesirken kalici hale getirir; biraz arkadas odalarina ugramak veya masadan kalkip kahve almak iyi gelecektir.",

            AnomalyType.HighDailyFatigue =>
                $"Gunluk yorgunluk skorun {anomaly.DailyFatigueScore:F0}/100 seviyesine ulasti. Hafizamdaki en iyi strateji su: {primaryMemory} Simdi 15 dakikalik bir goz ve su molasi verelim.",

            AnomalyType.EmotionalStress =>
                $"Bugun odaklanmak biraz zorlayici hissettirmis olabilir, bu cok normal. Hedefi mikro adimlara bolup 15 dakikalik sakin bir isinma seansi yapalim mi?",

            _ =>
                $"Seni dinliyorum {displayName}. Odak ritmin gayet dengeli gorunuyor. {primaryMemory} Bu donguyu koruyarak sonraki adimina hazirlanabiliriz."
        };
    }
}
