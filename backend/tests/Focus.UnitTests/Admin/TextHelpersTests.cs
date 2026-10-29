using Focus.Admin.Common;
using Xunit;

namespace Focus.UnitTests.Admin;

public class TextHelpersTests
{
    [Theory]
    [InlineData(null, "?")]
    [InlineData("", "?")]
    [InlineData("   ", "?")]
    [InlineData("ahmet", "A")]
    [InlineData("  zeynep", "Z")]
    public void Initial_ReturnsFirstLetterOrPlaceholder(string? name, string expected)
    {
        Assert.Equal(expected, TextHelpers.Initial(name));
    }

    [Fact]
    public void Initial_KeepsSurrogatePairsIntact()
    {
        Assert.Equal("\U0001F600", TextHelpers.Initial("\U0001F600 hi"));
    }
}
