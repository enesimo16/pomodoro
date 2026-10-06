using Focus.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class AdminStealthModeMiddlewareTests
{
    private readonly Mock<ILogger<AdminStealthModeMiddleware>> _loggerMock = new();
    private const string MasterKey = "focus_master_key_9971x";
    private const string SecondaryKey = "focus_admin_sec_2026";
    private const string Salt = "_focus_admin_stealth_salt_2026_x#99!";

    private IConfiguration CreateConfiguration(bool enabled = true, string[]? keys = null)
    {
        var keyList = keys ?? new[] { MasterKey, SecondaryKey };
        var inMemory = new Dictionary<string, string?>
        {
            ["AdminSecurity:Enabled"] = enabled.ToString(),
            ["AdminSecurity:CookieName"] = "focus_admin_passkey",
            ["AdminSecurity:CookieDays"] = "14",
            ["AdminSecurity:Keys:0"] = keyList.Length > 0 ? keyList[0] : "",
            ["AdminSecurity:Keys:1"] = keyList.Length > 1 ? keyList[1] : ""
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();
    }

    [Fact]
    public async Task InvokeAsync_WhenDisabled_ShouldPassToNext()
    {
        var config = CreateConfiguration(enabled: false);
        var nextCalled = false;
        var middleware = new AdminStealthModeMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            config,
            _loggerMock.Object);

        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenStaticAsset_ShouldPassToNext()
    {
        var config = CreateConfiguration(enabled: true);
        var nextCalled = false;
        var middleware = new AdminStealthModeMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            config,
            _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = "/css/site.css";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithoutKeyOrCookie_ShouldReturn404()
    {
        var config = CreateConfiguration(enabled: true);
        var nextCalled = false;
        var middleware = new AdminStealthModeMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            config,
            _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = "/ApiLab";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithValidKeyQueryParam_ShouldSetCookieAndRedirect()
    {
        var config = CreateConfiguration(enabled: true);
        var nextCalled = false;
        var middleware = new AdminStealthModeMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            config,
            _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = "/Jobs";
        context.Request.QueryString = new QueryString($"?admin_key={MasterKey}");

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/Jobs", context.Response.Headers.Location.ToString());

        var setCookieHeader = context.Response.Headers.SetCookie.ToString();
        Assert.Contains("focus_admin_passkey", setCookieHeader);
        Assert.Contains("httponly", setCookieHeader.ToLowerInvariant());
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidKeyQueryParam_ShouldReturn404()
    {
        var config = CreateConfiguration(enabled: true);
        var nextCalled = false;
        var middleware = new AdminStealthModeMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            config,
            _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = "/";
        context.Request.QueryString = new QueryString("?admin_key=hacker_attempt_123");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithValidCookie_ShouldPassToNext()
    {
        var config = CreateConfiguration(enabled: true);
        var nextCalled = false;
        var middleware = new AdminStealthModeMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            config,
            _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = "/ApiLab";

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(MasterKey + Salt));
        var validHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        context.Request.Headers.Append("Cookie", $"focus_admin_passkey={validHash}");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithValidHeader_ShouldPassToNext()
    {
        var config = CreateConfiguration(enabled: true);
        var nextCalled = false;
        var middleware = new AdminStealthModeMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            config,
            _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = "/ApiLab";
        context.Request.Headers.Append("X-Admin-Key", SecondaryKey);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithLogoutParam_ShouldDeleteCookieAndReturn404()
    {
        var config = CreateConfiguration(enabled: true);
        var nextCalled = false;
        var middleware = new AdminStealthModeMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            config,
            _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = "/";
        context.Request.QueryString = new QueryString("?admin_logout=true");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }
}
