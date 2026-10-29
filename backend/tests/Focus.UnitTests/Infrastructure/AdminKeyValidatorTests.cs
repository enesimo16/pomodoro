using Focus.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class AdminKeyValidatorTests
{
    private const string Key = "test-validator-key-cccccccc";

    private static IConfiguration CreateConfiguration(params string[] keys)
    {
        var values = new Dictionary<string, string?>();
        for (var i = 0; i < keys.Length; i++)
        {
            values[$"AdminSecurity:Keys:{i}"] = keys[i];
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void GetAllowedKeys_ShouldSkipBlankEntries()
    {
        var keys = AdminKeyValidator.GetAllowedKeys(CreateConfiguration(Key, "  ", ""));

        Assert.Equal(new[] { Key }, keys);
    }

    [Fact]
    public void IsValidKey_ShouldMatchTrimmedKeyAndRejectOthers()
    {
        var allowed = new[] { Key };

        Assert.True(AdminKeyValidator.IsValidKey($"  {Key} ", allowed));
        Assert.False(AdminKeyValidator.IsValidKey(Key + "x", allowed));
        Assert.False(AdminKeyValidator.IsValidKey("", allowed));
        Assert.False(AdminKeyValidator.IsValidKey(null, allowed));
    }

    [Fact]
    public void HasValidCredentials_WithHeaderOrCookie_ShouldSucceed()
    {
        var config = CreateConfiguration(Key);

        var headerContext = new DefaultHttpContext();
        headerContext.Request.Headers.Append(AdminKeyValidator.HeaderName, Key);
        Assert.True(AdminKeyValidator.HasValidCredentials(headerContext, config));

        var cookieContext = new DefaultHttpContext();
        cookieContext.Request.Headers.Append("Cookie", $"focus_admin_passkey={AdminKeyValidator.ComputeHash(Key)}");
        Assert.True(AdminKeyValidator.HasValidCredentials(cookieContext, config));
    }

    [Fact]
    public void HasValidCredentials_WithoutCredentialsOrKeys_ShouldFail()
    {
        Assert.False(AdminKeyValidator.HasValidCredentials(new DefaultHttpContext(), CreateConfiguration(Key)));

        var context = new DefaultHttpContext();
        context.Request.Headers.Append(AdminKeyValidator.HeaderName, Key);
        Assert.False(AdminKeyValidator.HasValidCredentials(context, CreateConfiguration()));
    }
}
