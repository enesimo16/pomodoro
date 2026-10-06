using Focus.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Focus.Infrastructure.Middleware;

public class UserAccessTrackingMiddleware
{
    private readonly RequestDelegate _next;

    public UserAccessTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserAccessTrackingService trackingService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (!IsStaticOrIgnoredPath(path))
        {
            var ip = GetClientIp(context);
            var ua = context.Request.Headers.UserAgent.ToString();

            Guid? userId = null;
            var subClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(subClaim, out var parsedId))
            {
                userId = parsedId;
            }

            try
            {
                await trackingService.RecordAccessAsync(userId, ip, ua, path);
            }
            catch
            {
                // Telemetri ana istek akisini kesintiye ugratmaz
            }
        }

        await _next(context);
    }

    private static bool IsStaticOrIgnoredPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        var lower = path.ToLowerInvariant();
        return lower.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
               lower.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
               lower.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
               lower.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
               lower.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
               lower.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
               lower.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
               lower.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase) ||
               lower.Contains("/swagger/", StringComparison.OrdinalIgnoreCase) ||
               lower.Contains("/lib/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}
