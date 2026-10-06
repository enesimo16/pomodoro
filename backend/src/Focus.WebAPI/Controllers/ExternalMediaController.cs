using Focus.Application.Common.Interfaces;
using Focus.Application.Features.ExternalMedia.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Focus.WebAPI.Controllers;

[Route("api/v1/media")]
public class ExternalMediaController : BaseApiController
{
    private readonly IThemeVideoService _videoService;
    private readonly IMusicTrackService _musicService;
    private readonly IWeatherService _weatherService;

    public ExternalMediaController(
        IThemeVideoService videoService,
        IMusicTrackService musicService,
        IWeatherService weatherService)
    {
        _videoService = videoService;
        _musicService = musicService;
        _weatherService = weatherService;
    }

    /// <summary>
    /// Pixabay API üzerinden döngüsel HD video arka plan temalarını (yağmur, şömine, kafe, gece şehri vb.) arar ve listeler.
    /// </summary>
    [HttpGet("themes")]
    [ProducesResponseType(typeof(IReadOnlyList<ThemeVideoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThemeVideos([FromQuery] string query = "rain window", [FromQuery] int perPage = 6, CancellationToken cancellationToken = default)
    {
        var videos = await _videoService.SearchVideosAsync(query, perPage, cancellationToken);
        return Ok(videos);
    }

    /// <summary>
    /// Jamendo API üzerinden piksel plak çalarda oynatılabilecek telifsiz Lo-Fi, caz ve enstrümantal müzik parçalarını arar.
    /// </summary>
    [HttpGet("tracks")]
    [ProducesResponseType(typeof(IReadOnlyList<MusicTrackDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTracks([FromQuery] string query = "lofi", [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var tracks = await _musicService.SearchTracksAsync(query, limit, cancellationToken);
        return Ok(tracks);
    }

    /// <summary>
    /// Open-Meteo API ile kullanıcının konumundaki canlı hava durumunu çeker; pencere yağmur efektini ve önerilen oda temasını hesaplar.
    /// </summary>
    [HttpGet("weather")]
    [ProducesResponseType(typeof(WeatherReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeather(
        [FromQuery] double latitude = 41.0082,
        [FromQuery] double longitude = 28.9784,
        [FromQuery] int? weatherCode = null,
        CancellationToken cancellationToken = default)
    {
        var weather = await _weatherService.GetCurrentWeatherAsync(latitude, longitude, weatherCode, cancellationToken);
        return Ok(weather);
    }
}
