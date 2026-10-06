namespace Focus.Application.Common.Interfaces;

public interface IBackgroundJobService
{
    Task<int> RunDailyStreakMaintenanceAsync(CancellationToken cancellationToken = default);
    Task<int> RunWeeklyStreakFreezeRefreshAsync(CancellationToken cancellationToken = default);
    Task<int> RunAbandonedSessionReconciliationAsync(CancellationToken cancellationToken = default);
    Task<int> RunGuestAndAuditCleanupAsync(CancellationToken cancellationToken = default);
    Task<int> RunGlobalAgentMemoryAggregationAsync(CancellationToken cancellationToken = default);
}
