namespace Focus.Application.Features.ExternalMedia.DTOs;

public record ThemeVideoDto(
    long Id,
    string PageUrl,
    int Duration,
    string ThumbnailUrl,
    string MediumVideoUrl,
    string LargeVideoUrl,
    string Tags);

public record MusicTrackDto(
    string Id,
    string Name,
    int Duration,
    string ArtistName,
    string AudioUrl,
    string? ImageUrl,
    string? LicenseCcUrl);

public record WeatherReportDto(
    double Temperature,
    double WindSpeed,
    int WeatherCode,
    string WeatherCondition,
    string SuggestedTheme,
    bool IsDay,
    DateTime Time);
