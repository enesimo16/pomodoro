namespace Focus.Infrastructure.Common;

public static class CorsOriginPolicy
{
    public static string[] ParseAllowedOrigins(string? configuredOrigins)
    {
        return (configuredOrigins ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeOrigin)
            .ToArray();
    }

    // Gelistirme ortaminda her localhost/127.0.0.1 kaynagina izin verilir; diger ortamlarda yalnizca tam eslesme gecerlidir
    public static bool IsOriginAllowed(string? origin, IReadOnlyCollection<string> allowedOrigins, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(origin)) return false;

        var normalized = NormalizeOrigin(origin);
        if (allowedOrigins.Contains(normalized, StringComparer.OrdinalIgnoreCase)) return true;

        if (!isDevelopment) return false;

        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && (uri.Host == "localhost" || uri.Host == "127.0.0.1");
    }

    private static string NormalizeOrigin(string origin)
    {
        return origin.Trim().TrimEnd('/');
    }
}
