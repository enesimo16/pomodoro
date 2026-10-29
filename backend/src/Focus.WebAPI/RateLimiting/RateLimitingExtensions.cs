using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Focus.WebAPI.RateLimiting;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Ai = "ai";
    public const string Media = "media";
    public const string Reaction = "reaction";
}

public static class RateLimitingExtensions
{
    public static IServiceCollection AddFocusRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingSettings>().Bind(configuration.GetSection(RateLimitingSettings.SectionName));

        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingSettings>>((options, settings) =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = (context, _) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }

                    return ValueTask.CompletedTask;
                };

                options.AddPolicy(RateLimitPolicies.Auth, context => Partition(RateLimitPolicies.Auth, ClientIp(context), settings.Value.Auth));
                options.AddPolicy(RateLimitPolicies.Ai, context => Partition(RateLimitPolicies.Ai, UserIdOrIp(context), settings.Value.Ai));
                options.AddPolicy(RateLimitPolicies.Media, context => Partition(RateLimitPolicies.Media, ClientIp(context), settings.Value.Media));
                options.AddPolicy(RateLimitPolicies.Reaction, context => Partition(RateLimitPolicies.Reaction, UserIdOrIp(context), settings.Value.Reaction));
            });

        return services;
    }

    private static RateLimitPartition<string> Partition(string policy, string key, RateLimitRule rule)
    {
        return RateLimitPartition.GetFixedWindowLimiter($"{policy}:{key}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rule.PermitLimit,
            Window = TimeSpan.FromSeconds(rule.WindowSeconds),
            QueueLimit = 0
        });
    }

    private static string ClientIp(HttpContext context)
    {
        return "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }

    private static string UserIdOrIp(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value;
        return string.IsNullOrEmpty(userId) ? ClientIp(context) : "user:" + userId;
    }
}
