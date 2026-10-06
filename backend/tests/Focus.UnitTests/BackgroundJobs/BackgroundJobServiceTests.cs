using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Focus.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Focus.UnitTests.BackgroundJobs;

public class BackgroundJobServiceTests
{
    private readonly Mock<IApplicationDbContext> _context;
    private readonly Mock<IVectorMemoryService> _vectorService;
    private readonly Mock<ILogger<BackgroundJobService>> _logger;
    private readonly BackgroundJobService _service;

    public BackgroundJobServiceTests()
    {
        _context = new Mock<IApplicationDbContext>();
        _vectorService = new Mock<IVectorMemoryService>();
        _logger = new Mock<ILogger<BackgroundJobService>>();
        _service = new BackgroundJobService(_context.Object, _vectorService.Object, _logger.Object);
    }

    [Fact]
    public void RecurringJobsConfigurator_ShouldRegisterFiveCoreJobs()
    {
        var jobs = RecurringJobsConfigurator.RegisteredJobs;

        Assert.Equal(5, jobs.Count);
        Assert.Contains(jobs, j => j.Id == "daily-streak-maintenance");
        Assert.Contains(jobs, j => j.Id == "weekly-freeze-refresh");
        Assert.Contains(jobs, j => j.Id == "abandoned-session-reconciliation");
        Assert.Contains(jobs, j => j.Id == "guest-audit-cleanup");
        Assert.Contains(jobs, j => j.Id == "global-memory-aggregation");
    }

    [Fact]
    public void UserStreak_ApplyOvernightFreezeOrReset_ShouldUseFreeze_WhenYesterdayMissed()
    {
        var streak = UserStreak.CreateDefault(Guid.NewGuid());
        // Record day before yesterday
        var twoDaysAgo = DateTime.UtcNow.Date.AddDays(-2);
        streak.RecordActivity(twoDaysAgo);
        Assert.Equal(1, streak.CurrentStreak);
        Assert.Equal(1, streak.FreezesAvailable);

        var (freezeUsed, reset) = streak.ApplyOvernightFreezeOrReset(DateTime.UtcNow.Date);

        Assert.True(freezeUsed);
        Assert.False(reset);
        Assert.Equal(0, streak.FreezesAvailable);
        Assert.Equal(1, streak.CurrentStreak);
    }

    [Fact]
    public void UserStreak_ApplyOvernightFreezeOrReset_ShouldReset_WhenNoFreezeAvailable()
    {
        var streak = UserStreak.CreateDefault(Guid.NewGuid());
        var threeDaysAgo = DateTime.UtcNow.Date.AddDays(-3);
        streak.RecordActivity(threeDaysAgo);

        // 1. gece: dondurucu devreye girer
        var (freezeUsed1, reset1) = streak.ApplyOvernightFreezeOrReset(DateTime.UtcNow.Date.AddDays(-1));
        Assert.True(freezeUsed1);
        Assert.False(reset1);
        Assert.Equal(0, streak.FreezesAvailable);
        Assert.Equal(1, streak.CurrentStreak);

        // 2. gece: dondurucu kalmadığı için seri sıfırlanır
        var (freezeUsed2, reset2) = streak.ApplyOvernightFreezeOrReset(DateTime.UtcNow.Date);

        Assert.False(freezeUsed2);
        Assert.True(reset2);
        Assert.Equal(0, streak.CurrentStreak);
    }

    [Fact]
    public void UserStreak_AddFreeze_ShouldNotExceedMaxLimit()
    {
        var streak = UserStreak.CreateDefault(Guid.NewGuid());
        Assert.Equal(1, streak.FreezesAvailable);

        var added1 = streak.AddFreeze(1);
        Assert.True(added1);
        Assert.Equal(2, streak.FreezesAvailable);

        var added2 = streak.AddFreeze(1);
        Assert.False(added2);
        Assert.Equal(2, streak.FreezesAvailable);
    }
}
