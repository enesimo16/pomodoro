using Hangfire.Dashboard;

namespace Focus.Infrastructure.BackgroundJobs;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}
