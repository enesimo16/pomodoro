using System.Net;
using System.Security.Claims;
using Focus.Application.Common.Interfaces;
using Focus.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class UserAccessTrackingMiddlewareTests
{
    private readonly Mock<IUserAccessTrackingService> _trackingMock = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private int _scopedResolutions;

    public UserAccessTrackingMiddlewareTests()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ =>
        {
            _scopedResolutions++;
            return _trackingMock.Object;
        });
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private UserAccessTrackingMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new UserAccessTrackingMiddleware(next, _scopeFactory, NullLogger<UserAccessTrackingMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateContext(Guid? userId, string path = "/api/v1/auth/me")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Headers.UserAgent = "UnitTestAgent/1.0";
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");

        if (userId.HasValue)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "test"));
        }

        return context;
    }

    private void VerifyNeverRecorded()
    {
        _trackingMock.Verify(t => t.RecordAccessAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ForAnonymousRequest_ShouldNotRecordAccess()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(CreateContext(userId: null));

        Assert.True(nextCalled);
        VerifyNeverRecorded();
    }

    [Fact]
    public async Task InvokeAsync_ForAuthenticatedRequest_ShouldRecordConnectionIpIgnoringForwardedHeaders()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext(userId);
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.99";
        context.Request.Headers["X-Real-IP"] = "198.51.100.98";
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        _trackingMock.Verify(t => t.RecordAccessAsync(userId, "203.0.113.7", "UnitTestAgent/1.0", "/api/v1/auth/me", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, _scopedResolutions);
    }

    [Fact]
    public async Task InvokeAsync_ForStaticPath_ShouldNotRecordAccess()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(CreateContext(Guid.NewGuid(), "/swagger/index.html"));

        VerifyNeverRecorded();
    }

    [Fact]
    public async Task InvokeAsync_WhenTrackingFails_ShouldStillCallNext()
    {
        _trackingMock
            .Setup(t => t.RecordAccessAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(CreateContext(Guid.NewGuid()));

        Assert.True(nextCalled);
    }
}
