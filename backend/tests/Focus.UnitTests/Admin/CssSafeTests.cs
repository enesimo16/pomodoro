using Focus.Admin.Common;
using Xunit;

namespace Focus.UnitTests.Admin;

public class CssSafeTests
{
    [Theory]
    [InlineData("#00fff5")]
    [InlineData("#ABCDEF")]
    public void Color_KeepsValidHexValues(string value)
    {
        Assert.Equal(value, CssSafe.Color(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("red")]
    [InlineData("#fff")]
    [InlineData("#00fff5; position: fixed")]
    [InlineData("#00fff5\n")]
    [InlineData("url(javascript:alert(1))")]
    [InlineData("#gggggg")]
    public void Color_ReplacesAnythingElseWithTheFallback(string? value)
    {
        Assert.Equal(CssSafe.FallbackColor, CssSafe.Color(value));
    }

    [Fact]
    public void Color_UsesTheSuppliedFallback()
    {
        Assert.Equal("#101010", CssSafe.Color("nope", "#101010"));
    }
}
