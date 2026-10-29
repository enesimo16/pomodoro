using Focus.Domain.Common;
using Xunit;

namespace Focus.UnitTests.Common;

public class SecureCodeTests
{
    [Fact]
    public void Generate_ShouldPrefixAndUseUnambiguousAlphabet()
    {
        for (var i = 0; i < 200; i++)
        {
            var code = SecureCode.Generate("FOC-");

            Assert.StartsWith("FOC-", code, StringComparison.Ordinal);
            var body = code["FOC-".Length..];
            Assert.Equal(8, body.Length);
            Assert.DoesNotContain(body, c => "01ILO".Contains(c, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Generate_ShouldNotRepeatAcrossManyCalls()
    {
        var codes = Enumerable.Range(0, 500).Select(_ => SecureCode.Generate("INV-", 10)).ToHashSet();

        Assert.Equal(500, codes.Count);
    }
}
