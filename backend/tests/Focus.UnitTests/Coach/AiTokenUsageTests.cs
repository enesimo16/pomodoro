using FluentAssertions;
using Focus.Domain.Entities;
using Xunit;

namespace Focus.UnitTests.Coach;

public class AiTokenUsageTests
{
    [Fact]
    public void TryConsume_WithinBudget_AccumulatesAndRotatesStamp()
    {
        var usage = AiTokenUsage.Create(Guid.NewGuid(), new DateOnly(2026, 10, 29));
        var stamp = usage.ConcurrencyStamp;

        usage.TryConsume(600, 1000).Should().BeTrue();
        usage.TryConsume(400, 1000).Should().BeTrue();

        usage.TokensUsed.Should().Be(1000);
        usage.ConcurrencyStamp.Should().NotBe(stamp);
    }

    [Fact]
    public void TryConsume_OverBudget_RejectsWithoutChangingUsage()
    {
        var usage = AiTokenUsage.Create(Guid.NewGuid(), new DateOnly(2026, 10, 29));
        usage.TryConsume(900, 1000);

        usage.TryConsume(101, 1000).Should().BeFalse();

        usage.TokensUsed.Should().Be(900);
    }
}
