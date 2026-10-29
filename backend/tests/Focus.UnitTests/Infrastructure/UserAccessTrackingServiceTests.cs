using FluentAssertions;
using Focus.Infrastructure.Services;
using Focus.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class UserAccessTrackingServiceTests
{
    [Fact]
    public async Task RecordAccessAsync_ShouldTruncateValuesToColumnLimits()
    {
        using var context = TestDbContextFactory.Create();
        var service = new UserAccessTrackingService(context);

        await service.RecordAccessAsync(
            Guid.NewGuid(),
            new string('1', 200),
            "Mozilla/5.0 " + new string('a', 3000),
            "/" + new string('p', 1000));

        var session = await context.UserDeviceSessions.SingleAsync();
        session.IpAddress.Length.Should().Be(UserAccessTrackingService.MaxIpLength);
        session.UserAgent.Length.Should().Be(UserAccessTrackingService.MaxUserAgentLength);
        session.LastPath!.Length.Should().Be(UserAccessTrackingService.MaxPathLength);
    }

    [Fact]
    public async Task RecordAccessAsync_WithSameOversizedUserAgent_ShouldReuseExistingSession()
    {
        using var context = TestDbContextFactory.Create();
        var service = new UserAccessTrackingService(context);
        var userId = Guid.NewGuid();
        var userAgent = "Mozilla/5.0 " + new string('a', 3000);

        await service.RecordAccessAsync(userId, "203.0.113.7", userAgent, "/a");
        await service.RecordAccessAsync(userId, "203.0.113.7", userAgent, "/b");

        var sessions = await context.UserDeviceSessions.ToListAsync();
        sessions.Should().ContainSingle();
        sessions[0].LastPath.Should().Be("/b");
    }
}
