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
        float[]? embedding,
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
        float[]? embedding,
        int importance = 3,
        CancellationToken cancellationToken = default)
    {
        var memory = AgentMemory.CreateGlobal(category, content, embedding, importance);
        _context.AgentMemories.Add(memory);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<MemoryInsightDto>> QueryHybridMemoriesAsync(
        Guid userId,
        float[]? queryEmbedding,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
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

        List<(AgentMemory Memory, double Relevance)> privateMemories;
        List<(AgentMemory Memory, double Relevance)> globalMemories;

        if (queryEmbedding != null && _context.Database.IsNpgsql())
        {
            var targetVector = new Vector(queryEmbedding);

            privateMemories = await QueryByVectorAsync(m => m.UserId == userId, targetVector, privateLimit, cancellationToken);
            globalMemories = await QueryByVectorAsync(m => m.UserId == null, targetVector, globalLimit, cancellationToken);
        }
        else
        {
            privateMemories = await QueryByImportanceAsync(m => m.UserId == userId, queryEmbedding, privateLimit, cancellationToken);
            globalMemories = await QueryByImportanceAsync(m => m.UserId == null, queryEmbedding, globalLimit, cancellationToken);
        }

        var result = new List<MemoryInsightDto>();

        foreach (var (m, relevance) in privateMemories)
        {
            m.RecordUsage();
            result.Add(new MemoryInsightDto(m.Id, false, m.Category, m.Content, m.Importance, relevance));
        }

        foreach (var (m, relevance) in globalMemories)
        {
            m.RecordUsage();
            result.Add(new MemoryInsightDto(m.Id, true, m.Category, m.Content, m.Importance, relevance));
        }

        if (privateMemories.Count > 0 || globalMemories.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
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

        return null;
    }

    private async Task<List<(AgentMemory Memory, double Relevance)>> QueryByVectorAsync(
        System.Linq.Expressions.Expression<Func<AgentMemory, bool>> scope,
        Vector target,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await _context.AgentMemories
            .Where(scope)
            .Where(m => m.Embedding != null)
            .Select(m => new { Memory = m, Distance = m.Embedding!.CosineDistance(target) })
            .OrderBy(x => x.Distance)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(x => (x.Memory, 1.0 - x.Distance)).ToList();
    }

    private async Task<List<(AgentMemory Memory, double Relevance)>> QueryByImportanceAsync(
        System.Linq.Expressions.Expression<Func<AgentMemory, bool>> scope,
        float[]? queryEmbedding,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await _context.AgentMemories
            .Where(scope)
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(m => (m, queryEmbedding != null && m.Embedding != null ? CosineSimilarity(queryEmbedding, m.Embedding) : 0.0))
            .ToList();
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            return 0.0;
        }

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return normA > 0 && normB > 0 ? dot / (Math.Sqrt(normA) * Math.Sqrt(normB)) : 0.0;
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
