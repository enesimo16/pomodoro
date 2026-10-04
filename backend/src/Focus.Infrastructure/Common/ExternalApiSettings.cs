namespace Focus.Infrastructure.Common;

public class ExternalApiSettings
{
    public const string SectionName = "ExternalApis";

    public PixabaySettings Pixabay { get; set; } = new();
    public JamendoSettings Jamendo { get; set; } = new();
    public OpenMeteoSettings OpenMeteo { get; set; } = new();
}

public class PixabaySettings
{
    public string ApiKey { get; set; } = string.Empty;
}

public class JamendoSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class OpenMeteoSettings
{
    public string BaseUrl { get; set; } = "https://api.open-meteo.com/v1";
}
