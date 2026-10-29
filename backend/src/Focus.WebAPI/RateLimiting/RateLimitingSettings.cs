namespace Focus.WebAPI.RateLimiting;

public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public RateLimitRule Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public RateLimitRule Ai { get; set; } = new() { PermitLimit = 20, WindowSeconds = 60 };
    public RateLimitRule Media { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };
}

public class RateLimitRule
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}
