using Focus.Infrastructure.Common;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class CorsOriginPolicyTests
{
    private static readonly string[] Allowed = CorsOriginPolicy.ParseAllowedOrigins("https://app.example.com/, http://localhost:3000");

    [Fact]
    public void ParseAllowedOrigins_ShouldTrimAndStripTrailingSlash()
    {
        Assert.Equal(new[] { "https://app.example.com", "http://localhost:3000" }, Allowed);
        Assert.Empty(CorsOriginPolicy.ParseAllowedOrigins(null));
    }

    [Theory]
    [InlineData("http://localhost:4321")]
    [InlineData("http://127.0.0.1:9999")]
    [InlineData("https://app.example.com")]
    public void IsOriginAllowed_InDevelopment_ShouldAllowAnyLocalhostAndConfiguredOrigins(string origin)
    {
        Assert.True(CorsOriginPolicy.IsOriginAllowed(origin, Allowed, isDevelopment: true));
    }

    [Theory]
    [InlineData("http://localhost:4321")]
    [InlineData("http://127.0.0.1:3000")]
    [InlineData("https://evil.example.com")]
    [InlineData("https://app.example.com.evil.com")]
    [InlineData("")]
    public void IsOriginAllowed_OutsideDevelopment_ShouldRejectUnlistedOrigins(string origin)
    {
        Assert.False(CorsOriginPolicy.IsOriginAllowed(origin, Allowed, isDevelopment: false));
    }

    [Theory]
    [InlineData("https://app.example.com")]
    [InlineData("https://APP.example.com/")]
    [InlineData("http://localhost:3000")]
    public void IsOriginAllowed_OutsideDevelopment_ShouldAllowExactConfiguredOrigins(string origin)
    {
        Assert.True(CorsOriginPolicy.IsOriginAllowed(origin, Allowed, isDevelopment: false));
    }
}
