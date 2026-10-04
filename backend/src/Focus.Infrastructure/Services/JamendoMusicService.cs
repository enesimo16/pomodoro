using System.Net.Http.Json;
using System.Text.Json;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.ExternalMedia.DTOs;
using Focus.Infrastructure.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.Services;

public class JamendoMusicService : IMusicTrackService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiSettings _settings;
    private readonly ILogger<JamendoMusicService> _logger;

    public JamendoMusicService(
        HttpClient httpClient,
        IOptions<ExternalApiSettings> options,
        ILogger<JamendoMusicService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MusicTrackDto>> GetLofiTracksAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var clientId = _settings.Jamendo.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogWarning("Jamendo Client ID is not configured.");
            return GetFallbackTracks();
        }

        var validLimit = Math.Clamp(limit, 1, 30);
        var url = $"https://api.jamendo.com/v3.0/tracks/?client_id={clientId}&format=json&tags=lofi&limit={validLimit}&audioformat=mp32";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Jamendo API returned {StatusCode}: {Reason}", response.StatusCode, response.ReasonPhrase);
                return GetFallbackTracks();
            }

            var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
            if (doc == null || !doc.RootElement.TryGetProperty("results", out var resultsElement))
            {
                return GetFallbackTracks();
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

            return list.Count > 0 ? list : GetFallbackTracks();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tracks from Jamendo");
            return GetFallbackTracks();
        }
    }

    private static IReadOnlyList<MusicTrackDto> GetFallbackTracks()
    {
        return new List<MusicTrackDto>
        {
            new(
                "demo-1",
                "Midnight Lofi Study",
                180,
                "Focus Chill Lab",
                "https://cdn.pixabay.com/download/audio/2022/05/27/audio_1808fbf07a.mp3",
                "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=300",
                "https://creativecommons.org/licenses/by/4.0/"),
            new(
                "demo-2",
                "Rainy Coffee Shop Vibes",
                210,
                "Pixel Beats",
                "https://cdn.pixabay.com/download/audio/2022/01/18/audio_d0a13f69d2.mp3",
                "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=300",
                "https://creativecommons.org/licenses/by/4.0/"),
            new(
                "demo-3",
                "Deep Flow Session",
                240,
                "Retro Horizon",
                "https://cdn.pixabay.com/download/audio/2022/10/14/audio_9939f772dd.mp3",
                "https://images.unsplash.com/photo-1518495973542-4542c06a5843?w=300",
                "https://creativecommons.org/licenses/by/4.0/")
        };
    }
}
