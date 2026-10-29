using Focus.Domain.Common;
using Xunit;

namespace Focus.UnitTests.Progression;

public class LevelCurveTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 455)]
    [InlineData(5, 1970)]
    [InlineData(10, 5972)]
    [InlineData(25, 25870)]
    [InlineData(50, 78423)]
    [InlineData(100, 237734)]
    public void XpRequiredForLevel_FollowsDocumentedCurve(int level, long expectedXp)
    {
        Assert.Equal(expectedXp, LevelCurve.XpRequiredForLevel(level));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(454, 1)]
    [InlineData(455, 2)]
    [InlineData(1969, 4)]
    [InlineData(1970, 5)]
    [InlineData(5972, 10)]
    [InlineData(237734, 100)]
    public void LevelForXp_IsInverseOfThresholds(long xp, int expectedLevel)
    {
        Assert.Equal(expectedLevel, LevelCurve.LevelForXp(xp));
    }

    [Fact]
    public void LevelForXp_IsConsistentWithThresholdsForEveryLevel()
    {
        for (var level = 2; level <= 150; level++)
        {
            var threshold = LevelCurve.XpRequiredForLevel(level);
            Assert.Equal(level, LevelCurve.LevelForXp(threshold));
            Assert.Equal(level - 1, LevelCurve.LevelForXp(threshold - 1));
        }
    }
}
