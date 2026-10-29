using Focus.Infrastructure.Middleware;
using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Focus.Infrastructure.BackgroundJobs;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        if (!configuration.GetValue("AdminSecurity:Enabled", true))
        {
            return environment.IsDevelopment();
        }

        return AdminKeyValidator.HasValidCredentials(httpContext, configuration);
    }
}
