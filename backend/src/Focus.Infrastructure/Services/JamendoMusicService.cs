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
            return GetFallbackTracks(trimmedQuery, validPage, validLimit);
        }

        var url = $"https://api.jamendo.com/v3.0/tracks/?client_id={clientId}&format=json&search={Uri.EscapeDataString(trimmedQuery)}&limit={validLimit}&offset={offset}&audioformat=mp32&order=popularity_total";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Jamendo API returned {StatusCode}: {Reason}", response.StatusCode, response.ReasonPhrase);
                return GetFallbackTracks(query);
            }

            var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
            if (doc == null || !doc.RootElement.TryGetProperty("results", out var resultsElement))
            {
                return GetFallbackTracks(query);
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

            return list.Count > 0 ? list : GetFallbackTracks(trimmedQuery, validPage, validLimit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tracks from Jamendo");
            return GetFallbackTracks(trimmedQuery, validPage, validLimit);
        }
    }

    private static List<MusicTrackDto> GetFallbackTracks(string query = "lofi", int page = 1, int limit = 12)
    {
        var allTracks = new List<MusicTrackDto>
        {
            new("demo-1", "Chill Flow (Deep Lo-Fi)", 180, "Focus Chill Lab", "https://cdn.pixabay.com/download/audio/2022/05/27/audio_1808fbf07a.mp3", "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-2", "Rainy Coffee Shop Vibes", 210, "Pixel Beats", "https://cdn.pixabay.com/download/audio/2022/01/18/audio_d0a13f69d2.mp3", "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-3", "Deep Study Synth Beats", 240, "Retro Horizon", "https://cdn.pixabay.com/download/audio/2022/10/14/audio_9939f772dd.mp3", "https://images.unsplash.com/photo-1518495973542-4542c06a5843?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-4", "Midnight Coding Terminal", 225, "Cyberpunk Ambient", "https://cdn.pixabay.com/download/audio/2022/03/15/audio_c8c8a73467.mp3", "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-5", "Alpha Wave Zen Meditation", 300, "Neuroflow Acoustics", "https://cdn.pixabay.com/download/audio/2021/08/04/audio_bb630cc098.mp3", "https://images.unsplash.com/photo-1518241353330-0f7941c2d9b5?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-6", "Quiet Japanese Garden Flow", 195, "Kyoto Soundscape", "https://cdn.pixabay.com/download/audio/2022/02/07/audio_d04a60b943.mp3", "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-7", "Rain & Brown Noise Shield", 270, "Acoustic Shielding", "https://cdn.pixabay.com/download/audio/2022/11/06/audio_c4314c9c14.mp3", "https://images.unsplash.com/photo-1519692933481-e162a57d6721?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-8", "Cosmic Horizon Ambient Drone", 320, "Orbit Lab", "https://cdn.pixabay.com/download/audio/2022/08/02/audio_884fe92c21.mp3", "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-9", "Old Library Wooden Piano", 205, "Baroque Focus", "https://cdn.pixabay.com/download/audio/2022/05/16/audio_c68c2d1b76.mp3", "https://images.unsplash.com/photo-1507842229451-7f01be45c067?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-10", "Winter Cabin Hearth Chill", 230, "Nordic Beats", "https://cdn.pixabay.com/download/audio/2022/01/26/audio_d463b3be93.mp3", "https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-11", "Autumn Evening Jazz Cafe", 215, "Vintage Trio", "https://cdn.pixabay.com/download/audio/2021/09/06/audio_88c750b2aa.mp3", "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=300", "https://creativecommons.org/licenses/by/4.0/"),
            new("demo-12", "8-Bit Nostalgia Retro Drive", 190, "Pixel Arcade", "https://cdn.pixabay.com/download/audio/2022/03/10/audio_c295754516.mp3", "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=300", "https://creativecommons.org/licenses/by/4.0/")
        };

        var filtered = string.IsNullOrWhiteSpace(query) || query.Equals("lofi", StringComparison.OrdinalIgnoreCase)
            ? allTracks
            : allTracks.Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || t.ArtistName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filtered.Count == 0) filtered = allTracks;

        return filtered
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();
    }
}
