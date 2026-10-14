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
        int perPage = 12,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _settings.Pixabay.ApiKey;
        var validPage = Math.Max(1, page);
        var validPerPage = Math.Clamp(perPage, 3, 50);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Pixabay API key is not configured.");
            return GetFallbackVideos(query, validPage, validPerPage);
        }

        var url = $"https://pixabay.com/api/videos/?key={apiKey}&q={Uri.EscapeDataString(query)}&video_type=film&per_page={validPerPage}&page={validPage}";

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

            return list.Count > 0 ? list : GetFallbackVideos(query, validPage, validPerPage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch videos from Pixabay");
            return GetFallbackVideos(query, validPage, validPerPage);
        }
    }

    private static IReadOnlyList<ThemeVideoDto> GetFallbackVideos(string query = "", int page = 1, int perPage = 12)
    {
        var allFallbacks = new List<ThemeVideoDto>
        {
            new(101, "https://pixabay.com/videos/id-101/", 30, "https://images.unsplash.com/photo-1519692933481-e162a57d6721?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4", "rain, window, cozy, focus, drizzle"),
            new(102, "https://pixabay.com/videos/id-102/", 45, "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4", "cafe, evening, warm ambient, coffee shop, study"),
            new(103, "https://pixabay.com/videos/id-103/", 60, "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerFun.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerFun.mp4", "cyberpunk, neon, night city, synthwave, terminal"),
            new(104, "https://pixabay.com/videos/id-104/", 40, "https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerJoyBlazes.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerJoyBlazes.mp4", "fireplace, wood cabin, crackle, winter warmth"),
            new(105, "https://pixabay.com/videos/id-105/", 50, "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerMeltdowns.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerMeltdowns.mp4", "zen, japanese garden, bamboo, water fountain, calm"),
            new(106, "https://pixabay.com/videos/id-106/", 90, "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4", "space station, orbit, earth, cosmic calm, stars"),
            new(107, "https://pixabay.com/videos/id-107/", 35, "https://images.unsplash.com/photo-1448375240586-882707db888b?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/SubaruOutbackSeeTheWorld.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/SubaruOutbackSeeTheWorld.mp4", "forest, mist, pine trees, morning breeze, green"),
            new(108, "https://pixabay.com/videos/id-108/", 55, "https://images.unsplash.com/photo-1507842229451-7f01be45c067?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/TearsOfSteel.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/TearsOfSteel.mp4", "library, ancient books, quiet reading, wood desk"),
            new(109, "https://pixabay.com/videos/id-109/", 48, "https://images.unsplash.com/photo-1514565131-fce0801e5785?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/WeAreGoingOnBullrun.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/WeAreGoingOnBullrun.mp4", "sunset, skyline, golden hour, rooftop, evening"),
            new(110, "https://pixabay.com/videos/id-110/", 65, "https://images.unsplash.com/photo-1517411032315-54ef2cb783bb?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/WhatCarCanYouGetForAGrand.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/WhatCarCanYouGetForAGrand.mp4", "snow, gentle blizzard, frosted window, winter stillness"),
            new(111, "https://pixabay.com/videos/id-111/", 42, "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4", "midnight, minimalist moon, monochrome dark, deep code"),
            new(112, "https://pixabay.com/videos/id-112/", 38, "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=640", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4", "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4", "retro 80s, crt glow, vaporwave, pixel arcade, beats")
        };

        var filtered = string.IsNullOrWhiteSpace(query)
            ? allFallbacks
            : allFallbacks.Where(f => f.Tags.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filtered.Count == 0) filtered = allFallbacks;

        return filtered
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToList();
    }
}
