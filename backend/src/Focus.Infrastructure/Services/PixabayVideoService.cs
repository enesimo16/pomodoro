using System.Net.Http.Json;
using System.Text.Json;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.ExternalMedia.DTOs;
using Focus.Infrastructure.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.Services;

public class PixabayVideoService : IThemeVideoService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiSettings _settings;
    private readonly ILogger<PixabayVideoService> _logger;

    public PixabayVideoService(
        HttpClient httpClient,
        IOptions<ExternalApiSettings> options,
        ILogger<PixabayVideoService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) FocusPlatform/1.0");
        }
    }

    public async Task<IReadOnlyList<ThemeVideoDto>> SearchVideosAsync(
        string query = "rain window",
        int perPage = 6,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _settings.Pixabay.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Pixabay API key is not configured.");
            return GetFallbackVideos();
        }

        var validPerPage = Math.Clamp(perPage, 3, 20);
        var url = $"https://pixabay.com/api/videos/?key={apiKey}&q={Uri.EscapeDataString(query)}&video_type=film&per_page={validPerPage}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pixabay API returned {StatusCode}: {Reason}", response.StatusCode, response.ReasonPhrase);
                return GetFallbackVideos();
            }

            var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
            if (doc == null || !doc.RootElement.TryGetProperty("hits", out var hitsElement))
            {
                return GetFallbackVideos();
            }

            var list = new List<ThemeVideoDto>();
            foreach (var hit in hitsElement.EnumerateArray())
            {
                var id = hit.GetProperty("id").GetInt64();
                var pageUrl = hit.TryGetProperty("pageURL", out var pu) ? pu.GetString() ?? "" : "";
                var duration = hit.TryGetProperty("duration", out var dur) ? dur.GetInt32() : 0;
                var tags = hit.TryGetProperty("tags", out var tg) ? tg.GetString() ?? "" : "";
                var pictureId = hit.TryGetProperty("picture_id", out var pic) ? pic.GetString() ?? "" : "";

                var thumbnailUrl = !string.IsNullOrEmpty(pictureId)
                    ? $"https://i.vimeocdn.com/video/{pictureId}_640x360.jpg"
                    : "";

                var mediumVideoUrl = "";
                var largeVideoUrl = "";

                if (hit.TryGetProperty("videos", out var videos))
                {
                    if (videos.TryGetProperty("medium", out var med))
                    {
                        mediumVideoUrl = med.TryGetProperty("url", out var mUrl) ? mUrl.GetString() ?? "" : "";
                        if (string.IsNullOrEmpty(thumbnailUrl) && med.TryGetProperty("thumbnail", out var mThumb))
                        {
                            thumbnailUrl = mThumb.GetString() ?? "";
                        }
                    }

                    if (videos.TryGetProperty("large", out var lrg))
                    {
                        largeVideoUrl = lrg.TryGetProperty("url", out var lUrl) ? lUrl.GetString() ?? "" : "";
                    }
                }

                if (string.IsNullOrEmpty(largeVideoUrl))
                {
                    largeVideoUrl = mediumVideoUrl;
                }

                if (!string.IsNullOrEmpty(mediumVideoUrl))
                {
                    list.Add(new ThemeVideoDto(
                        id,
                        pageUrl,
                        duration,
                        thumbnailUrl,
                        mediumVideoUrl,
                        largeVideoUrl,
                        tags));
                }
            }

            return list.Count > 0 ? list : GetFallbackVideos();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch videos from Pixabay");
            return GetFallbackVideos();
        }
    }

    private static IReadOnlyList<ThemeVideoDto> GetFallbackVideos()
    {
        return new List<ThemeVideoDto>
        {
            new(
                101,
                "https://pixabay.com/videos/id-101/",
                30,
                "https://images.unsplash.com/photo-1519692933481-e162a57d6721?w=640",
                "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4",
                "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4",
                "rain, cozy window, coffee, focus"),
            new(
                102,
                "https://pixabay.com/videos/id-102/",
                45,
                "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=640",
                "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4",
                "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4",
                "cafe, evening, warm ambient, study")
        };
    }
}
