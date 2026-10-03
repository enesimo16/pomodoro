namespace Focus.Infrastructure.Authentication;

public class GoogleAuthSettings
{
    public const string SectionName = "Auth:Google";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}
