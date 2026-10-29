using System.Net.Http.Json;
using System.Text.Json;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.ExternalMedia.DTOs;
using Focus.Infrastructure.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.Services;

public class PixabayVideoService : IThemeVideoService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PixabayVideoService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public PixabayVideoService(
        HttpClient httpClient,
        IOptions<ExternalApiSettings> options,
        IMemoryCache cache,
        ILogger<PixabayVideoService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _cache = cache;
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
            return [];
        }

        var trimmedQuery = (query ?? string.Empty).Trim();
        var cacheKey = $"pixabay:{trimmedQuery.ToLowerInvariant()}:{validPerPage}:{validPage}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ThemeVideoDto>? cached) && cached != null)
        {
            return cached;
        }

        var url = $"https://pixabay.com/api/videos/?key={apiKey}&q={Uri.EscapeDataString(trimmedQuery)}&video_type=film&per_page={validPerPage}&page={validPage}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pixabay API returned {StatusCode}", (int)response.StatusCode);
                return [];
            }

            using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
            if (doc == null || !doc.RootElement.TryGetProperty("hits", out var hitsElement))
            {
                _logger.LogWarning("Pixabay API returned an unexpected payload.");
                return [];
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

            _cache.Set(cacheKey, (IReadOnlyList<ThemeVideoDto>)list, CacheDuration);
            return list;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            _logger.LogWarning(ex, "Failed to fetch videos from Pixabay");
            return [];
        }
    }
}
