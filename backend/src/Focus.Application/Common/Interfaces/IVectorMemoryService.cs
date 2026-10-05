using Focus.Application.Features.Coach.DTOs;
using Focus.Domain.Enums;

namespace Focus.Application.Common.Interfaces;

public interface IVectorMemoryService
{
    Task AddPrivateMemoryAsync(Guid userId, MemoryCategory category, string content, float[] embedding, int importance = 3, CancellationToken cancellationToken = default);
    Task AddGlobalMemoryAsync(MemoryCategory category, string content, float[] embedding, int importance = 3, CancellationToken cancellationToken = default);
    Task<List<MemoryInsightDto>> QueryHybridMemoriesAsync(Guid userId, float[] queryEmbedding, int topK = 3, CancellationToken cancellationToken = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
