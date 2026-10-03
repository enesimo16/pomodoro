namespace Focus.Infrastructure.Authentication;

public class JwtSettings
{
    public const string SectionName = "Auth:Jwt";

    public string SigningKey { get; set; } = "focus_default_dev_secret_key_needs_override_in_env_32_bytes";
    public string Issuer { get; set; } = "focus-api";
    public string Audience { get; set; } = "focus-web";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
