using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.ExternalMedia.DTOs;
using Focus.Infrastructure.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.Services;

public class OpenMeteoWeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenMeteoWeatherService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public OpenMeteoWeatherService(
        HttpClient httpClient,
        IOptions<ExternalApiSettings> options,
        IMemoryCache cache,
        ILogger<OpenMeteoWeatherService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WeatherReportDto?> GetCurrentWeatherAsync(
        double latitude = 41.0082,
        double longitude = 28.9784,
        int? simulatedWeatherCode = null,
        CancellationToken cancellationToken = default)
    {
        // Eger kullanici ozel bir hava durumunu simule etmek istediyse dogrudan onu dondur
        if (simulatedWeatherCode.HasValue)
        {
            var simCode = simulatedWeatherCode.Value;
            var simIsDay = true;
            var simDetails = InterpretWeatherCode(simCode, simIsDay);
            var simTemp = simCode switch
            {
                >= 71 and <= 86 => -2.5,
                >= 95 => 16.0,
                >= 51 and <= 67 => 14.5,
                0 => 24.0,
                _ => 19.0
            };

            return new WeatherReportDto(
                simTemp,
                15.0,
                simCode,
                simDetails.Condition,
                simDetails.SuggestedTheme,
                simDetails.WindowEffect,
                simDetails.Soundscape,
                simDetails.LightingColor,
                simDetails.Description,
                simIsDay,
                DateTime.UtcNow);
        }

        var roundedLatitude = Math.Round(latitude, 2);
        var roundedLongitude = Math.Round(longitude, 2);
        var cacheKey = string.Create(CultureInfo.InvariantCulture, $"weather:{roundedLatitude}:{roundedLongitude}");
        if (_cache.TryGetValue(cacheKey, out WeatherReportDto? cached) && cached != null)
        {
            return cached;
        }

        var baseUrl = string.IsNullOrWhiteSpace(_settings.OpenMeteo.BaseUrl)
            ? "https://api.open-meteo.com/v1"
            : _settings.OpenMeteo.BaseUrl.TrimEnd('/');

        var url = string.Create(CultureInfo.InvariantCulture, $"{baseUrl}/forecast?latitude={roundedLatitude}&longitude={roundedLongitude}&current_weather=true");

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Open-Meteo returned status {StatusCode}", (int)response.StatusCode);
                return null;
            }

            using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
            if (doc == null || !doc.RootElement.TryGetProperty("current_weather", out var cw))
            {
                _logger.LogWarning("Open-Meteo returned an unexpected payload.");
                return null;
            }

            if (!cw.TryGetProperty("temperature", out var t) || !cw.TryGetProperty("weathercode", out var c))
            {
                _logger.LogWarning("Open-Meteo payload is missing temperature or weather code.");
                return null;
            }

            var temp = t.GetDouble();
            var wind = cw.TryGetProperty("windspeed", out var w) ? w.GetDouble() : 0.0;
            var code = c.GetInt32();
            var isDay = cw.TryGetProperty("is_day", out var id) && id.GetInt32() == 1;
            var timeStr = cw.TryGetProperty("time", out var tm) ? tm.GetString() : null;

            var time = DateTime.TryParse(
                timeStr,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedTime)
                ? parsedTime
                : DateTime.UtcNow;
            var details = InterpretWeatherCode(code, isDay);

            var report = new WeatherReportDto(
                temp,
                wind,
                code,
                details.Condition,
                details.SuggestedTheme,
                details.WindowEffect,
                details.Soundscape,
                details.LightingColor,
                details.Description,
                isDay,
                time);

            _cache.Set(cacheKey, report, CacheDuration);
            return report;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException or FormatException)
        {
            _logger.LogWarning(ex, "Failed to fetch weather from Open-Meteo");
            return null;
        }
    }

    private static (string Condition, string SuggestedTheme, string WindowEffect, string Soundscape, string LightingColor, string Description) InterpretWeatherCode(int code, bool isDay)
    {
        return code switch
        {
            0 => (
                isDay ? "Güneşli / Açık" : "Açık Yıldızlı Gece",
                isDay ? "sunny_loft" : "night_lofi",
                isDay ? "sunny_rays" : "starry_sky",
                isDay ? "Sabah Kuş Sesleri & Hafif Esinti" : "Gece Cırcır Böcekleri & Lo-Fi Beat",
                isDay ? "#FFD166" : "#2E3A59",
                isDay ? "Pırıl pırıl güneşli bir gün. Güneş ışınları çalışma masasını aydınlatıyor." : "Sessiz ve berrak bir gece. Yıldızlar pencereden parıldıyor."
            ),
            1 => (
                "Az Bulutlu",
                "breeze_loft",
                "light_clouds",
                "Hafif Park Esintisi & Kafe Uğultusu",
                "#FFEAA7",
                "Hafif beyaz bulutlar süzülüyor. Zihni dinlendiren tazeleyici bir hava."
            ),
            2 => (
                "Parçalı Bulutlu",
                "cozy_cloudy",
                "partly_clouds",
                "Kahve Dükkanı Ambiyansı & Yumuşak Rüzgar",
                "#DFE6E9",
                "Gökyüzü parçalı bulutlu. Ne çok sıcak ne soğuk, çalışma için en rahat saatler."
            ),
            3 => (
                "Kapalı / Yoğun Bulutlu",
                "slate_minimal",
                "overcast_sky",
                "Derin Odak Beyaz Gürültüsü & Uzak Rüzgar",
                "#B2BEC3",
                "Gri ve bulutlu bir gökyüzü. Dış etkenlerden izole derin çalışma ortamı."
            ),
            45 or 48 => (
                "Sisli ve Puslu",
                "foggy_morning",
                "fog_haze",
                "Yumuşak Pembe Gürültü (Pink Noise)",
                "#CED6E0",
                "Pencerenin dışı yoğun sis tabakasıyla kaplı. Sanki bulutların üzerinde çalışıyorsunuz."
            ),
            51 or 53 or 55 => (
                "Hafif Çisenti",
                "drizzle_window",
                "light_rain_drops",
                "Cama Vuran Çisenti & Akustik Gitar",
                "#74B9FF",
                "Pencere camına ince çisenti damlaları vuruyor. Huzurlu bir çalışma ritmi."
            ),
            61 or 63 or 65 or 80 or 81 or 82 => (
                "Sağanak Yağmurlu",
                "rainy_window",
                "rain_drops",
                "Pencereye Vuran Yağmur & Lo-Fi Piyano",
                "#0984E3",
                "Dışarıda sağanak yağmur akıp gidiyor. Sıcak kahve eşliğinde masa lambası yanıyor."
            ),
            71 or 73 or 75 or 77 or 85 or 86 => (
                "Lapa Lapa Karlı",
                "snowy_cabin",
                "snow_flakes",
                "Çıtırtılı Şömine & Kış Rüzgarı",
                "#F5F6FA",
                "Dışarıda bembeyaz kar taneleri süzülüyor. Oda sıcak ve korunaklı bir sığınak."
            ),
            95 or 96 or 99 => (
                "Gök Gürültülü Fırtına",
                "thunder_room",
                "thunder_flash",
                "Şiddetli Yağmur & Derin Gök Gürültüsü",
                "#2D3436",
                "Gökyüzünde şimşekler çakıyor, bardaktan boşanırcasına yağıyor. Derin akış kalkanı aktif!"
            ),
            _ => (
                "Sakin & Dingin",
                "default_room",
                "calm_room",
                "Retro Plak Çalar Ambiyansı",
                "#F1F2F6",
                "Oda sessiz ve huzurlu."
            )
        };
    }
}
