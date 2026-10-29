using FluentAssertions;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Xunit;

namespace Focus.UnitTests.Coach;

public class AgentMemoryEntityTests
{
    [Fact]
    public void CreatePrivate_SetsUserIdAndIsGlobalFalse()
    {
        var userId = Guid.NewGuid();
        var embedding = new float[768];

        var memory = AgentMemory.CreatePrivate(userId, MemoryCategory.PersonalHabit, "Erken saatte odaklaniyor.", embedding, importance: 4);

        memory.UserId.Should().Be(userId);
        memory.IsGlobal.Should().BeFalse();
        memory.Category.Should().Be(MemoryCategory.PersonalHabit);
        memory.Importance.Should().Be(4);
        memory.UsageCount.Should().Be(0);
        memory.LastAccessedAt.Should().BeNull();
    }

    [Fact]
    public void CreateGlobal_SetsUserIdNullAndIsGlobalTrue()
    {
        var embedding = new float[768];

        var memory = AgentMemory.CreateGlobal(MemoryCategory.DeepWorkStrategy, "45 dakika odak verimlidir.", embedding, importance: 5);

        memory.UserId.Should().BeNull();
        memory.IsGlobal.Should().BeTrue();
        memory.Category.Should().Be(MemoryCategory.DeepWorkStrategy);
        memory.Importance.Should().Be(5);
    }

    [Fact]
    public void RecordUsage_IncrementsUsageCountAndUpdatesLastAccessedAt()
    {
        var memory = AgentMemory.CreateGlobal(MemoryCategory.FatigueRecovery, "Su molasi verin.", new float[768]);

        memory.RecordUsage();

        memory.UsageCount.Should().Be(1);
        memory.LastAccessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Create_TruncatesContentToColumnLimitAndAllowsMissingEmbedding()
    {
        var memory = AgentMemory.CreatePrivate(Guid.NewGuid(), MemoryCategory.PersonalHabit, new string('x', 3000), null);

        memory.Content.Length.Should().Be(AgentMemory.MaxContentLength);
        memory.Embedding.Should().BeNull();
    }

    [Fact]
    public void CreateGlobal_StoresSourceSessionId()
    {
        var sessionId = Guid.NewGuid();

        var memory = AgentMemory.CreateGlobal(MemoryCategory.CollectiveWisdom, "icerik", null, 4, sessionId);

        memory.SourceSessionId.Should().Be(sessionId);
    }
}
