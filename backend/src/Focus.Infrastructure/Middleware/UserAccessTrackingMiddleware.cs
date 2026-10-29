using System.Security.Claims;
using Focus.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Focus.Infrastructure.Middleware;

public class UserAccessTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserAccessTrackingMiddleware> _logger;

    public UserAccessTrackingMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<UserAccessTrackingMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var userId = GetUserId(context.User);

        if (userId.HasValue && !IsStaticOrIgnoredPath(path))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var ua = context.Request.Headers.UserAgent.ToString();

            try
            {
                // Takip yazimi istegin DbContext'ini etkilememesi icin kendi DI kapsaminda calisir
                using var scope = _scopeFactory.CreateScope();
                var trackingService = scope.ServiceProvider.GetRequiredService<IUserAccessTrackingService>();
                await trackingService.RecordAccessAsync(userId, ip, ua, path, context.RequestAborted);
            }
            catch (Exception ex)
            {
                // Telemetri ana istek akisini kesintiye ugratmaz
                _logger.LogWarning(ex, "Kullanici erisim kaydi yazilamadi.");
            }
        }

        await _next(context);
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(subClaim, out var parsedId) ? parsedId : null;
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
}
