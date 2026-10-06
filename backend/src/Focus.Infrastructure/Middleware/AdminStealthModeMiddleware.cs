using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Focus.Infrastructure.Middleware;

/// <summary>
/// Admin Stealth Mode Middleware (Görünmezlik & Yetki Kalkanı):
/// Yetkisiz kullanıcılar ve tarayıcı botları için paneli tamamen yokmuş gibi gösterir (HTTP 404).
/// Geçerli bir admin anahtarı ile girildiğinde güvenli HttpOnly çerez tanımlar ve temiz URL'e yönlendirir.
/// </summary>
public class AdminStealthModeMiddleware
{
    private const string Salt = "_focus_admin_stealth_salt_2026_x#99!";
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminStealthModeMiddleware> _logger;

    public AdminStealthModeMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<AdminStealthModeMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var enabled = _configuration.GetValue("AdminSecurity:Enabled", true);
        if (!enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // Statik dosyaları (css, js, font) geçir
        if (IsStaticAsset(path))
        {
            await _next(context);
            return;
        }

        var allowedKeys = _configuration.GetSection("AdminSecurity:Keys")
            .Get<string[]>() ?? Array.Empty<string>();

        var cookieName = _configuration.GetValue("AdminSecurity:CookieName", "focus_admin_passkey")!;
        var cookieDays = _configuration.GetValue("AdminSecurity:CookieDays", 14);

        // 1. Çıkış / Kilitleme İsteği (?admin_logout=true)
        if (context.Request.Query.ContainsKey("admin_logout"))
        {
            context.Response.Cookies.Delete(cookieName);
            await ReturnStealthNotFoundAsync(context);
            return;
        }

        // 2. Query String Üzerinden Anahtar Kontrolü (?admin_key=...)
        if (context.Request.Query.TryGetValue("admin_key", out var queryKeyVal))
        {
            var providedKey = queryKeyVal.ToString().Trim();
            if (IsValidKey(providedKey, allowedKeys))
            {
                var hash = ComputeHash(providedKey);
                context.Response.Cookies.Append(cookieName, hash, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(cookieDays),
                    Path = "/"
                });

                // URL'den anahtarı temizlemek için yönlendir (güvenlik & omuz gözetlemesi önleme)
                var cleanUrl = BuildCleanUrl(context);
                context.Response.Redirect(cleanUrl);
                return;
            }

            // Hatalı anahtar girilirse direkt 404 dön (admin paneli olduğunu ele vermez)
            await ReturnStealthNotFoundAsync(context);
            return;
        }

        // 3. HTTP Header Üzerinden Anahtar Kontrolü (X-Admin-Key)
        if (context.Request.Headers.TryGetValue("X-Admin-Key", out var headerKeyVal))
        {
            var providedKey = headerKeyVal.ToString().Trim();
            if (IsValidKey(providedKey, allowedKeys))
            {
                await _next(context);
                return;
            }

            await ReturnStealthNotFoundAsync(context);
            return;
        }

        // 4. HttpOnly Çerez Doğrulama
        if (context.Request.Cookies.TryGetValue(cookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal))
        {
            if (IsValidCookieHash(cookieVal, allowedKeys))
            {
                await _next(context);
                return;
            }

            // Geçersiz / süresi dolmuş / anahtarı iptal edilmiş çerezi temizle
            context.Response.Cookies.Delete(cookieName);
        }

        // Yetkisiz erişim: 401 veya 403 değil, kasıtlı olarak 404 Not Found dönülür (Stealth Mode)
        await ReturnStealthNotFoundAsync(context);
    }

    private static bool IsValidKey(string key, string[] allowedKeys)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        return allowedKeys.Any(k => string.Equals(k.Trim(), key, StringComparison.Ordinal));
    }

    private static bool IsValidCookieHash(string hash, string[] allowedKeys)
    {
        if (string.IsNullOrWhiteSpace(hash)) return false;
        foreach (var key in allowedKeys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            var expectedHash = ComputeHash(key.Trim());
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(hash),
                    Encoding.UTF8.GetBytes(expectedHash)))
            {
                return true;
            }
        }
        return false;
    }

    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input + Salt);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string BuildCleanUrl(HttpContext context)
    {
        var queryCollection = context.Request.Query
            .Where(q => !string.Equals(q.Key, "admin_key", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var path = context.Request.Path.Value ?? "/";
        if (queryCollection.Count == 0)
        {
            return path;
        }

        var queryString = QueryString.Create(queryCollection.Select(q => 
            new KeyValuePair<string, string?>(q.Key, q.Value.ToString())));

        return $"{path}{queryString.ToUriComponent()}";
    }

    private static bool IsStaticAsset(string path)
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
               lower.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
               lower.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
               lower.StartsWith("/js/", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ReturnStealthNotFoundAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("404 Not Found");
    }
}
