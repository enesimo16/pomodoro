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
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Focus.Infrastructure.Services;

public class VectorMemoryService : IVectorMemoryService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly AiSettings _aiSettings;
    private readonly ILogger<VectorMemoryService> _logger;

    public VectorMemoryService(
        ApplicationDbContext context,
        HttpClient httpClient,
        IOptions<AiSettings> aiSettings,
        ILogger<VectorMemoryService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _aiSettings = aiSettings.Value;
        _logger = logger;
    }

    public async Task AddPrivateMemoryAsync(
        Guid userId,
        MemoryCategory category,
        string content,
        float[] embedding,
        int importance = 3,
        CancellationToken cancellationToken = default)
    {
        var memory = AgentMemory.CreatePrivate(userId, category, content, embedding, importance);
        _context.AgentMemories.Add(memory);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddGlobalMemoryAsync(
        MemoryCategory category,
        string content,
        float[] embedding,
        int importance = 3,
        CancellationToken cancellationToken = default)
    {
        var memory = AgentMemory.CreateGlobal(category, content, embedding, importance);
        _context.AgentMemories.Add(memory);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<MemoryInsightDto>> QueryHybridMemoriesAsync(
        Guid userId,
        float[] queryEmbedding,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        var targetVector = new Vector(queryEmbedding);

        // Kullanicinin kisisel hafiza sayisini denetle (Soguk Baslangic Karari)
        var userPrivateCount = await _context.AgentMemories
            .CountAsync(m => m.UserId == userId, cancellationToken);

        int privateLimit;
        int globalLimit;

        if (userPrivateCount < 5)
        {
            // Soguk Baslangic: Kolektif havuza oncelik ver
            globalLimit = Math.Max(2, topK);
            privateLimit = Math.Max(1, topK - 1);
        }
        else
        {
            // Olgun Profil: Kisisel havuza agirlik ver
            privateLimit = Math.Max(2, topK);
            globalLimit = 1;
        }

        List<AgentMemory> privateMemories;
        List<AgentMemory> globalMemories;

        if (_context.Database.IsNpgsql())
        {
            privateMemories = await _context.AgentMemories
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.Embedding.CosineDistance(targetVector))
                .Take(privateLimit)
                .ToListAsync(cancellationToken);

            globalMemories = await _context.AgentMemories
                .Where(m => m.UserId == null)
                .OrderBy(m => m.Embedding.CosineDistance(targetVector))
                .Take(globalLimit)
                .ToListAsync(cancellationToken);
        }
        else
        {
            privateMemories = await _context.AgentMemories
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.Importance)
                .Take(privateLimit)
                .ToListAsync(cancellationToken);

            globalMemories = await _context.AgentMemories
                .Where(m => m.UserId == null)
                .OrderByDescending(m => m.Importance)
                .Take(globalLimit)
                .ToListAsync(cancellationToken);
        }

        var result = new List<MemoryInsightDto>();

        foreach (var m in privateMemories)
        {
            m.RecordUsage();
            result.Add(new MemoryInsightDto(
                m.Id,
                false,
                m.Category,
                m.Content,
                m.Importance,
                0.95));
        }

        foreach (var m in globalMemories)
        {
            m.RecordUsage();
            result.Add(new MemoryInsightDto(
                m.Id,
                true,
                m.Category,
                m.Content,
                m.Importance,
                0.85));
        }

        if (privateMemories.Count > 0 || globalMemories.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return GlobalMemorySeeder.GenerateDeterministicEmbedding("empty");
        }

        // Gemini API anahtari tanimliysa gercek Gemini Embedding uret
        if (_aiSettings.IsGeminiConfigured)
        {
            try
            {
                var gemini = _aiSettings.Gemini;
                var payload = new
                {
                    model = $"models/{gemini.EmbeddingModelId}",
                    content = new
                    {
                        parts = new[] { new { text } }
                    },
                    outputDimensionality = gemini.EmbeddingDimensions
                };

                using var res = await _httpClient.PostAsJsonAsync($"models/{gemini.EmbeddingModelId}:embedContent", payload, cancellationToken);
                using var json = await GeminiResponse.ReadAsync(res, _logger, "embedding", cancellationToken);
                if (json != null
                    && json.RootElement.TryGetProperty("embedding", out var embeddingProp)
                    && embeddingProp.TryGetProperty("values", out var valuesProp)
                    && valuesProp.ValueKind == JsonValueKind.Array)
                {
                    var values = valuesProp.EnumerateArray().Select(v => (float)v.GetDouble()).ToArray();
                    if (values.Length == gemini.EmbeddingDimensions)
                    {
                        return Normalize(values);
                    }

                    _logger.LogWarning("Gemini embedding boyutu beklenenden farkli: {Actual} / {Expected}", values.Length, gemini.EmbeddingDimensions);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Gemini embedding istegi basarisiz oldu.");
            }
        }

        return GlobalMemorySeeder.GenerateDeterministicEmbedding(text);
    }

    private static float[] Normalize(float[] values)
    {
        double sumSquares = 0;
        foreach (var v in values)
        {
            sumSquares += v * v;
        }

        var norm = (float)Math.Sqrt(sumSquares);
        if (norm <= 0)
        {
            return values;
        }

        for (var i = 0; i < values.Length; i++)
        {
            values[i] /= norm;
        }

        return values;
    }
}
