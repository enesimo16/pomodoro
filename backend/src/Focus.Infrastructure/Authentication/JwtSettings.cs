using System.Text;

namespace Focus.Infrastructure.Authentication;

public class JwtSettings
{
    public const string SectionName = "Auth:Jwt";
    public const int MinSigningKeyBytes = 32;

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "focus-api";
    public string Audience { get; set; } = "focus-web";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;

    public static bool HasValidSigningKey(JwtSettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.SigningKey)
            && Encoding.UTF8.GetByteCount(settings.SigningKey) >= MinSigningKeyBytes;
    }
}
