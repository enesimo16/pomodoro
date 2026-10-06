using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Services;

public class UserAccessTrackingService : IUserAccessTrackingService
{
    private readonly IApplicationDbContext _context;

    public UserAccessTrackingService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RecordAccessAsync(
        Guid? userId,
        string ipAddress,
        string userAgent,
        string? path,
        CancellationToken cancellationToken = default)
    {
        var cleanIp = NormalizeIp(ipAddress);
        var ua = string.IsNullOrWhiteSpace(userAgent) ? "Bilinmeyen Tarayıcı" : userAgent.Trim();

        var (deviceType, os, browser) = ParseUserAgent(ua);
        var now = DateTime.UtcNow;

        var query = _context.UserDeviceSessions
            .Where(s => s.IpAddress == cleanIp && s.UserAgent == ua);

        if (userId.HasValue)
        {
            query = query.Where(s => s.UserId == userId.Value);
        }
        else
        {
            query = query.Where(s => s.UserId == null);
        }

        var existingSession = await query
            .OrderByDescending(s => s.LastSeenAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSession != null)
        {
            existingSession.RecordActivity(now, path);
            if (userId.HasValue && !existingSession.UserId.HasValue)
            {
                existingSession.AssociateUser(userId.Value);
            }
        }
        else
        {
            var newSession = UserDeviceSession.Create(
                userId,
                cleanIp,
                ua,
                deviceType,
                os,
                browser,
                path);

            _context.UserDeviceSessions.Add(newSession);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return "127.0.0.1";
        var cleaned = ip.Trim();
        if (cleaned == "::1" || cleaned.StartsWith("::ffff:127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return "127.0.0.1";
        }
        if (cleaned.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
        {
            return cleaned.Replace("::ffff:", "", StringComparison.OrdinalIgnoreCase);
        }
        return cleaned;
    }

    private static (string DeviceType, string Os, string Browser) ParseUserAgent(string ua)
    {
        var device = "Masaüstü";
        var os = "Diğer";
        var browser = "Diğer";

        var uaLower = ua.ToLowerInvariant();

        if (uaLower.Contains("ipad", StringComparison.OrdinalIgnoreCase) || uaLower.Contains("tablet", StringComparison.OrdinalIgnoreCase))
        {
            device = "Tablet";
        }
        else if (uaLower.Contains("mobile", StringComparison.OrdinalIgnoreCase) || 
                 uaLower.Contains("iphone", StringComparison.OrdinalIgnoreCase) || 
                 uaLower.Contains("android", StringComparison.OrdinalIgnoreCase))
        {
            device = "Mobil";
        }
        else
        {
            device = "Masaüstü";
        }

        if (uaLower.Contains("windows nt 10.0", StringComparison.OrdinalIgnoreCase) || uaLower.Contains("windows nt 11.0", StringComparison.OrdinalIgnoreCase))
            os = "Windows 11 / 10";
        else if (uaLower.Contains("windows", StringComparison.OrdinalIgnoreCase))
            os = "Windows";
        else if (uaLower.Contains("mac os x", StringComparison.OrdinalIgnoreCase) || uaLower.Contains("macintosh", StringComparison.OrdinalIgnoreCase))
            os = "macOS";
        else if (uaLower.Contains("iphone os", StringComparison.OrdinalIgnoreCase) || uaLower.Contains("ios", StringComparison.OrdinalIgnoreCase))
            os = "iOS";
        else if (uaLower.Contains("android", StringComparison.OrdinalIgnoreCase))
            os = "Android";
        else if (uaLower.Contains("linux", StringComparison.OrdinalIgnoreCase))
            os = "Linux";

        if (uaLower.Contains("edg/", StringComparison.OrdinalIgnoreCase))
            browser = "Microsoft Edge";
        else if (uaLower.Contains("chrome/", StringComparison.OrdinalIgnoreCase) && !uaLower.Contains("edg/", StringComparison.OrdinalIgnoreCase))
            browser = "Google Chrome";
        else if (uaLower.Contains("firefox/", StringComparison.OrdinalIgnoreCase))
            browser = "Mozilla Firefox";
        else if (uaLower.Contains("safari/", StringComparison.OrdinalIgnoreCase) && !uaLower.Contains("chrome/", StringComparison.OrdinalIgnoreCase))
            browser = "Apple Safari";
        else if (uaLower.Contains("opr/", StringComparison.OrdinalIgnoreCase) || uaLower.Contains("opera", StringComparison.OrdinalIgnoreCase))
            browser = "Opera";

        return (device, os, browser);
    }
}
