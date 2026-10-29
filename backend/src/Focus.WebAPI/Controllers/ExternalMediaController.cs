using Focus.Application.Common.Interfaces;
using Focus.Application.Features.ExternalMedia.DTOs;
using Focus.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Focus.WebAPI.Controllers;

[Route("api/v1/media")]
[EnableRateLimiting(RateLimitPolicies.Media)]
public class ExternalMediaController : BaseApiController
{
    private readonly IThemeVideoService _videoService;
    private readonly IMusicTrackService _musicService;
    private readonly IWeatherService _weatherService;
    private readonly IHostEnvironment _environment;

    private const int MaxQueryLength = 100;

    public ExternalMediaController(
        IThemeVideoService videoService,
        IMusicTrackService musicService,
        IWeatherService weatherService,
        IHostEnvironment environment)
    {
        _environment = environment;
        _videoService = videoService;
        _musicService = musicService;
        _weatherService = weatherService;
    }

    /// <summary>
    /// Pixabay API üzerinden döngüsel HD video arka plan temalarını (yağmur, şömine, kafe, gece şehri vb.) arar ve listeler.
    /// </summary>
    [HttpGet("themes")]
    [ProducesResponseType(typeof(IReadOnlyList<ThemeVideoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThemeVideos(
        [FromQuery] string query = "rain window",
        [FromQuery] int perPage = 12,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (query is { Length: > MaxQueryLength })
        {
            return BadRequest(new { message = $"Arama ifadesi en fazla {MaxQueryLength} karakter olabilir." });
        }

        var videos = await _videoService.SearchVideosAsync(query, perPage, page, cancellationToken);
        return Ok(videos);
    }

    /// <summary>
    /// Jamendo API üzerinden piksel plak çalarda oynatılabilecek telifsiz Lo-Fi, caz ve enstrümantal müzik parçalarını arar.
    /// </summary>
    [HttpGet("tracks")]
    [ProducesResponseType(typeof(IReadOnlyList<MusicTrackDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTracks(
        [FromQuery] string query = "lofi",
        [FromQuery] int limit = 12,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (query is { Length: > MaxQueryLength })
        {
            return BadRequest(new { message = $"Arama ifadesi en fazla {MaxQueryLength} karakter olabilir." });
        }

        var tracks = await _musicService.SearchTracksAsync(query, limit, page, cancellationToken);
        return Ok(tracks);
    }

    /// <summary>
    /// Open-Meteo API ile kullanıcının konumundaki canlı hava durumunu çeker; pencere yağmur efektini ve önerilen oda temasını hesaplar.
    /// </summary>
    [HttpGet("weather")]
    [ProducesResponseType(typeof(WeatherReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetWeather(
        [FromQuery] double latitude = 41.0082,
        [FromQuery] double longitude = 28.9784,
        [FromQuery] int? weatherCode = null,
        CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90 || !double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            return BadRequest(new { message = "Enlem -90 ile 90, boylam -180 ile 180 araliginda olmalidir." });
        }

        // Hava durumu simulasyonu yalnizca gelistirme ortaminda calisir
        var simulatedCode = _environment.IsDevelopment() ? weatherCode : null;
        var weather = await _weatherService.GetCurrentWeatherAsync(latitude, longitude, simulatedCode, cancellationToken);
        if (weather == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Hava durumu servisine su an ulasilamiyor." });
        }

        return Ok(weather);
    }
}
