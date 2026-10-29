using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Focus.Infrastructure.Middleware;

/// <summary>
/// Admin Stealth Mode Middleware (Görünmezlik & Yetki Kalkanı):
/// Yetkisiz kullanıcılar ve tarayıcı botları için paneli tamamen yokmuş gibi gösterir (HTTP 404).
/// Geçerli bir admin anahtarı ile girildiğinde güvenli HttpOnly çerez tanımlar ve temiz URL'e yönlendirir.
/// </summary>
public class AdminStealthModeMiddleware
{
    private static readonly string[] StaticRoots = { "/lib/", "/css/", "/js/" };

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AdminStealthModeMiddleware> _logger;

    public AdminStealthModeMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<AdminStealthModeMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _environment = environment;
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

        // Statik dosyaları (css, js, lib, favicon) geçir
        if (IsStaticAsset(path))
        {
            await _next(context);
            return;
        }

        var allowedKeys = AdminKeyValidator.GetAllowedKeys(_configuration);
        var cookieName = AdminKeyValidator.GetCookieName(_configuration);
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
            if (AdminKeyValidator.IsValidKey(providedKey, allowedKeys))
            {
                var hash = AdminKeyValidator.ComputeHash(providedKey);
                context.Response.Cookies.Append(cookieName, hash, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !_environment.IsDevelopment(),
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
            _logger.LogWarning("Gecersiz admin anahtari denemesi: {RemoteIp}", context.Connection.RemoteIpAddress);
            await ReturnStealthNotFoundAsync(context);
            return;
        }

        // 3. HTTP Header Üzerinden Anahtar Kontrolü (X-Admin-Key)
        if (context.Request.Headers.TryGetValue(AdminKeyValidator.HeaderName, out var headerKeyVal))
        {
            if (AdminKeyValidator.IsValidKey(headerKeyVal.ToString(), allowedKeys))
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
            if (AdminKeyValidator.IsValidCookieHash(cookieVal, allowedKeys))
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

        if (string.Equals(path, "/favicon.ico", StringComparison.OrdinalIgnoreCase)) return true;

        return StaticRoots.Any(root => path.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task ReturnStealthNotFoundAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("404 Not Found");
    }
}
