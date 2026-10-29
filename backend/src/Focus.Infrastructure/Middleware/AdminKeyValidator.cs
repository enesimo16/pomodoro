using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Focus.Infrastructure.Middleware;

public static class AdminKeyValidator
{
    public const string HeaderName = "X-Admin-Key";
    private const string Salt = "_focus_admin_stealth_salt_2026_x#99!";

    public static string[] GetAllowedKeys(IConfiguration configuration)
    {
        return (configuration.GetSection("AdminSecurity:Keys").Get<string[]>() ?? Array.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .ToArray();
    }

    public static string GetCookieName(IConfiguration configuration)
    {
        return configuration.GetValue("AdminSecurity:CookieName", "focus_admin_passkey")!;
    }

    // Tum anahtarlar sabit sureli SHA-256 ozetleri uzerinden karsilastirilir ve erken cikilmaz
    public static bool IsValidKey(string? key, string[] allowedKeys)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim()));
        var matched = false;
        foreach (var allowed in allowedKeys)
        {
            var allowedHash = SHA256.HashData(Encoding.UTF8.GetBytes(allowed.Trim()));
            matched |= CryptographicOperations.FixedTimeEquals(providedHash, allowedHash);
        }

        return matched;
    }

    public static bool IsValidCookieHash(string? hash, string[] allowedKeys)
    {
        if (string.IsNullOrWhiteSpace(hash)) return false;

        var providedBytes = Encoding.UTF8.GetBytes(hash);
        var matched = false;
        foreach (var key in allowedKeys)
        {
            var expectedBytes = Encoding.UTF8.GetBytes(ComputeHash(key.Trim()));
            matched |= CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
        }

        return matched;
    }

    public static string ComputeHash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input + Salt));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool HasValidCredentials(HttpContext context, IConfiguration configuration)
    {
        var allowedKeys = GetAllowedKeys(configuration);
        if (allowedKeys.Length == 0) return false;

        if (context.Request.Headers.TryGetValue(HeaderName, out var headerKey)
            && IsValidKey(headerKey.ToString(), allowedKeys))
        {
            return true;
        }

        return context.Request.Cookies.TryGetValue(GetCookieName(configuration), out var cookieValue)
            && IsValidCookieHash(cookieValue, allowedKeys);
    }
}
