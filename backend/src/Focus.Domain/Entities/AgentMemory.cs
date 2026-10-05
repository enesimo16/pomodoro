namespace Focus.Domain.Entities;

using Focus.Domain.Common;
using Focus.Domain.Enums;

public class AgentMemory : BaseEntity<Guid>, IAggregateRoot
{
    public Guid? UserId { get; private set; }
    public MemoryCategory Category { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public float[] Embedding { get; private set; } = Array.Empty<float>();
    public int Importance { get; private set; } = 3;
    public int UsageCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastAccessedAt { get; private set; }

    public bool IsGlobal => UserId == null;

    private AgentMemory() { }

    public static AgentMemory CreatePrivate(
        Guid userId,
        MemoryCategory category,
        string content,
        float[] embedding,
        int importance = 3)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Bellek icerigi bos olamaz.", nameof(content));

        return new AgentMemory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = category,
            Content = content.Trim(),
            Embedding = embedding,
            Importance = Math.Clamp(importance, 1, 5),
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static AgentMemory CreateGlobal(
        MemoryCategory category,
        string content,
        float[] embedding,
        int importance = 3)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Kolektif bellek icerigi bos olamaz.", nameof(content));

        return new AgentMemory
        {
            Id = Guid.NewGuid(),
            UserId = null,
            Category = category,
            Content = content.Trim(),
            Embedding = embedding,
            Importance = Math.Clamp(importance, 1, 5),
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RecordUsage()
    {
        UsageCount++;
        LastAccessedAt = DateTime.UtcNow;
    }

    public void UpdateEmbedding(float[] newEmbedding)
    {
        Embedding = newEmbedding;
    }
}
