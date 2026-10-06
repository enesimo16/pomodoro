using Focus.Application.Features.AdminAnalytics.DTOs;

namespace Focus.Application.Common.Interfaces;

public interface IAdminAnalyticsService
{
    Task<PlatformOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<UserAnalyticsDto> GetUserAnalyticsAsync(CancellationToken cancellationToken = default);
    Task<EconomyAnalyticsDto> GetEconomyAnalyticsAsync(CancellationToken cancellationToken = default);
    Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default);
}
