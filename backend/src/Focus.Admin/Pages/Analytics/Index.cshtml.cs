using Focus.Application.Common.Interfaces;
using Focus.Application.Features.AdminAnalytics.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Focus.Admin.Pages.Analytics;

public class IndexModel : PageModel
{
    private readonly IAdminAnalyticsService _analyticsService;

    public IndexModel(IAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public PlatformOverviewDto Overview { get; set; } = null!;
    public UserAnalyticsDto UserAnalytics { get; set; } = null!;
    public EconomyAnalyticsDto EconomyAnalytics { get; set; } = null!;
    public SystemHealthDto SystemHealth { get; set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Overview = await _analyticsService.GetOverviewAsync(cancellationToken);
        UserAnalytics = await _analyticsService.GetUserAnalyticsAsync(cancellationToken);
        EconomyAnalytics = await _analyticsService.GetEconomyAnalyticsAsync(cancellationToken);
        SystemHealth = await _analyticsService.GetSystemHealthAsync(cancellationToken);
    }
}
