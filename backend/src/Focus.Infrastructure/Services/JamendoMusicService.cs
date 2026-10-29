using System.Net.Http.Json;
using System.Text.Json;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.ExternalMedia.DTOs;
using Focus.Infrastructure.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.Services;

public class JamendoMusicService : IMusicTrackService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<JamendoMusicService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public JamendoMusicService(
        HttpClient httpClient,
        IOptions<ExternalApiSettings> options,
        IMemoryCache cache,
        ILogger<JamendoMusicService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public Task<IReadOnlyList<MusicTrackDto>> GetLofiTracksAsync(int limit = 12, int page = 1, CancellationToken cancellationToken = default)
    {
        return SearchTracksAsync("lofi", limit, page, cancellationToken);
    }

    public async Task<IReadOnlyList<MusicTrackDto>> SearchTracksAsync(
        string query = "lofi",
        int limit = 12,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var clientId = _settings.Jamendo.ClientId;
        var validPage = Math.Max(1, page);
        var validLimit = Math.Clamp(limit, 1, 50);
        var offset = (validPage - 1) * validLimit;
        var trimmedQuery = string.IsNullOrWhiteSpace(query) ? "lofi" : query.Trim();

        if (string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogWarning("Jamendo Client ID is not configured.");
            return [];
        }

        var cacheKey = $"jamendo:{trimmedQuery.ToLowerInvariant()}:{validLimit}:{validPage}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<MusicTrackDto>? cached) && cached != null)
        {
            return cached;
        }

        var url = $"https://api.jamendo.com/v3.0/tracks/?client_id={clientId}&format=json&search={Uri.EscapeDataString(trimmedQuery)}&limit={validLimit}&offset={offset}&audioformat=mp32&order=popularity_total";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Jamendo API returned {StatusCode}", (int)response.StatusCode);
                return [];
            }

            using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
            if (doc == null || !doc.RootElement.TryGetProperty("results", out var resultsElement))
            {
                _logger.LogWarning("Jamendo API returned an unexpected payload.");
                return [];
            }

            if (doc.RootElement.TryGetProperty("headers", out var headers)
                && headers.TryGetProperty("status", out var status)
                && status.ValueKind == JsonValueKind.String
                && string.Equals(status.GetString(), "failed", StringComparison.OrdinalIgnoreCase))
            {
                var message = headers.TryGetProperty("error_message", out var em) ? em.GetString() : null;
                _logger.LogWarning("Jamendo API reported a failure: {Message}", message);
                return [];
            }

            var list = new List<MusicTrackDto>();
            foreach (var item in resultsElement.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString() ?? "";
                var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "Unknown Track" : "Unknown Track";
                var duration = item.TryGetProperty("duration", out var d) ? d.GetInt32() : 0;
                var artistName = item.TryGetProperty("artist_name", out var a) ? a.GetString() ?? "Lofi Artist" : "Lofi Artist";
                var audioUrl = item.TryGetProperty("audio", out var au) ? au.GetString() ?? "" : "";
                var image = item.TryGetProperty("image", out var im) ? im.GetString() : null;
                var license = item.TryGetProperty("license_ccurl", out var lc) ? lc.GetString() : null;

                if (!string.IsNullOrEmpty(audioUrl))
                {
                    list.Add(new MusicTrackDto(
                        id,
                        name,
                        duration,
                        artistName,
                        audioUrl,
                        image,
                        license));
                }
            }

            _cache.Set(cacheKey, (IReadOnlyList<MusicTrackDto>)list, CacheDuration);
            return list;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            _logger.LogWarning(ex, "Failed to fetch tracks from Jamendo");
            return [];
        }
    }
}
