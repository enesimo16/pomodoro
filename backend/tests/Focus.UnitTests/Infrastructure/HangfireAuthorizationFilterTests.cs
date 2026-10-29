using Focus.Infrastructure.BackgroundJobs;
using Focus.Infrastructure.Middleware;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class HangfireAuthorizationFilterTests
{
    private const string Key = "test-hangfire-key-dddddddd";

    private static DashboardContext CreateDashboardContext(string environmentName, bool enabled, string[] keys, Action<HttpContext>? configureRequest = null)
    {
        var values = new Dictionary<string, string?> { ["AdminSecurity:Enabled"] = enabled.ToString() };
        for (var i = 0; i < keys.Length; i++)
        {
            values[$"AdminSecurity:Keys:{i}"] = keys[i];
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton(environment.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        configureRequest?.Invoke(httpContext);

        return new AspNetCoreDashboardContext(Mock.Of<JobStorage>(), new DashboardOptions(), httpContext);
    }

    [Fact]
    public void Authorize_WithoutCredentials_ShouldDeny()
    {
        var context = CreateDashboardContext("Production", enabled: true, new[] { Key });

        Assert.False(new HangfireAuthorizationFilter().Authorize(context));
    }

    [Fact]
    public void Authorize_WithValidAdminCookie_ShouldAllow()
    {
        var context = CreateDashboardContext("Production", enabled: true, new[] { Key },
            http => http.Request.Headers.Append("Cookie", $"focus_admin_passkey={AdminKeyValidator.ComputeHash(Key)}"));

        Assert.True(new HangfireAuthorizationFilter().Authorize(context));
    }

    [Fact]
    public void Authorize_WithInvalidAdminCookie_ShouldDeny()
    {
        var context = CreateDashboardContext("Production", enabled: true, new[] { Key },
            http => http.Request.Headers.Append("Cookie", "focus_admin_passkey=deadbeef"));

        Assert.False(new HangfireAuthorizationFilter().Authorize(context));
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Development", true)]
    public void Authorize_WhenAdminSecurityDisabled_ShouldAllowOnlyInDevelopment(string environmentName, bool expected)
    {
        var context = CreateDashboardContext(environmentName, enabled: false, Array.Empty<string>());

        Assert.Equal(expected, new HangfireAuthorizationFilter().Authorize(context));
    }
}
