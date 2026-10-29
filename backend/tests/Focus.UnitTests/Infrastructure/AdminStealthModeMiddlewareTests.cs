using Focus.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class AdminStealthModeMiddlewareTests
{
    private readonly Mock<ILogger<AdminStealthModeMiddleware>> _loggerMock = new();
    private readonly Mock<IHostEnvironment> _environmentMock = new();
    private const string MasterKey = "test-master-key-aaaaaaaa";
    private const string SecondaryKey = "test-secondary-key-bbbbbbbb";
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
            _environmentMock.Object,
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
            _environmentMock.Object,
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
            _environmentMock.Object,
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
            _environmentMock.Object,
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
            _environmentMock.Object,
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
            _environmentMock.Object,
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
            _environmentMock.Object,
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
            _environmentMock.Object,
            _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = "/";
        context.Request.QueryString = new QueryString("?admin_logout=true");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    private AdminStealthModeMiddleware CreateMiddleware(IConfiguration config, RequestDelegate next, string environmentName)
    {
        _environmentMock.SetupGet(e => e.EnvironmentName).Returns(environmentName);
        return new AdminStealthModeMiddleware(next, config, _environmentMock.Object, _loggerMock.Object);
    }

    [Theory]
    [InlineData("/js/site.js")]
    [InlineData("/css/site.css")]
    [InlineData("/lib/bootstrap/dist/js/bootstrap.min.js")]
    [InlineData("/favicon.ico")]
    public async Task InvokeAsync_WhenPathIsUnderStaticRoot_ShouldPassToNext(string path)
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(CreateConfiguration(), _ => { nextCalled = true; return Task.CompletedTask; }, "Production");

        var context = new DefaultHttpContext();
        context.Request.Path = path;

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData("/Jobs/export.js")]
    [InlineData("/ApiLab/secret.css")]
    [InlineData("/hangfire/logo.png")]
    [InlineData("/anything.ico")]
    public async Task InvokeAsync_WhenPathOnlyLooksLikeStaticFile_ShouldReturn404(string path)
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(CreateConfiguration(), _ => { nextCalled = true; return Task.CompletedTask; }, "Production");

        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithKeyDifferingOnlyInLastCharacter_ShouldReturn404()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(CreateConfiguration(), _ => { nextCalled = true; return Task.CompletedTask; }, "Production");

        var context = new DefaultHttpContext();
        context.Request.Path = "/ApiLab";
        context.Request.Headers.Append("X-Admin-Key", MasterKey[..^1] + "x");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithNoConfiguredKeys_ShouldRejectEvenEmptyKey()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(CreateConfiguration(keys: Array.Empty<string>()), _ => { nextCalled = true; return Task.CompletedTask; }, "Production");

        var context = new DefaultHttpContext();
        context.Request.Path = "/ApiLab";
        context.Request.Headers.Append("X-Admin-Key", "");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Development", false)]
    public async Task InvokeAsync_WithValidKey_ShouldSetSecureCookieOnlyOutsideDevelopment(string environmentName, bool expectSecure)
    {
        var middleware = CreateMiddleware(CreateConfiguration(), _ => Task.CompletedTask, environmentName);

        var context = new DefaultHttpContext();
        context.Request.Path = "/Jobs";
        context.Request.QueryString = new QueryString($"?admin_key={MasterKey}");

        await middleware.InvokeAsync(context);

        var setCookieHeader = context.Response.Headers.SetCookie.ToString().ToLowerInvariant();
        Assert.Equal(expectSecure, setCookieHeader.Contains("secure", StringComparison.Ordinal));
    }
}
