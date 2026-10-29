using Focus.Infrastructure.Authentication;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class JwtSettingsTests
{
    [Fact]
    public void DefaultSigningKey_ShouldBeEmptyAndInvalid()
    {
        var settings = new JwtSettings();

        Assert.Equal(string.Empty, settings.SigningKey);
        Assert.False(JwtSettings.HasValidSigningKey(settings));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("short-key", false)]
    [InlineData("0123456789012345678901234567890", false)]
    [InlineData("01234567890123456789012345678901", true)]
    public void HasValidSigningKey_ShouldRequireAtLeast32Bytes(string key, bool expected)
    {
        Assert.Equal(expected, JwtSettings.HasValidSigningKey(new JwtSettings { SigningKey = key }));
    }
}
