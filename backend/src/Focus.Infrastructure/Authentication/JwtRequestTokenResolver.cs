using Microsoft.AspNetCore.Http;

namespace Focus.Infrastructure.Authentication;

public static class JwtRequestTokenResolver
{
    public const string AccessTokenCookieName = "focus_at";

    // Authorization basligi varsa varsayilan Bearer okuyucusu devreye girsin diye null doner
    public static string? Resolve(HttpRequest request)
    {
        if (!string.IsNullOrEmpty(request.Headers.Authorization))
        {
            return null;
        }

        if (request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
        {
            var queryToken = request.Query["access_token"].ToString();
            if (!string.IsNullOrEmpty(queryToken))
            {
                return queryToken;
            }
        }

        var cookieToken = request.Cookies[AccessTokenCookieName];
        return string.IsNullOrEmpty(cookieToken) ? null : cookieToken;
    }
}
