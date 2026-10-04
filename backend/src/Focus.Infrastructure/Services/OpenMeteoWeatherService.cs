using System.Net.Http.Json;
using System.Text.Json;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.ExternalMedia.DTOs;
using Focus.Infrastructure.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.Services;

public class OpenMeteoWeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiSettings _settings;
    private readonly ILogger<OpenMeteoWeatherService> _logger;

    public OpenMeteoWeatherService(
        HttpClient httpClient,
        IOptions<ExternalApiSettings> options,
        ILogger<OpenMeteoWeatherService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<WeatherReportDto?> GetCurrentWeatherAsync(
        double latitude = 41.0082,
        double longitude = 28.9784,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_settings.OpenMeteo.BaseUrl)
            ? "https://api.open-meteo.com/v1"
            : _settings.OpenMeteo.BaseUrl.TrimEnd('/');

        var url = $"{baseUrl}/forecast?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current_weather=true";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Open-Meteo returned status {StatusCode}", response.StatusCode);
                return GetDefaultReport();
            }

            var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
            if (doc == null || !doc.RootElement.TryGetProperty("current_weather", out var cw))
            {
                return GetDefaultReport();
            }

            var temp = cw.TryGetProperty("temperature", out var t) ? t.GetDouble() : 20.0;
            var wind = cw.TryGetProperty("windspeed", out var w) ? w.GetDouble() : 5.0;
            var code = cw.TryGetProperty("weathercode", out var c) ? c.GetInt32() : 0;
            var isDay = cw.TryGetProperty("is_day", out var id) && id.GetInt32() == 1;
            var timeStr = cw.TryGetProperty("time", out var tm) ? tm.GetString() : null;

            var time = DateTime.TryParse(timeStr, out var parsedTime) ? parsedTime : DateTime.UtcNow;
            var (condition, suggestedTheme) = InterpretWeatherCode(code, isDay);

            return new WeatherReportDto(
                temp,
                wind,
                code,
                condition,
                suggestedTheme,
                isDay,
                time);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch weather from Open-Meteo");
            return GetDefaultReport();
        }
    }

    private static (string Condition, string SuggestedTheme) InterpretWeatherCode(int code, bool isDay)
    {
        return code switch
        {
            0 => (isDay ? "Güneşli / Açık" : "Açık Gece", isDay ? "sunny_loft" : "night_lofi"),
            1 or 2 or 3 => ("Parçalı Bulutlu", "cozy_cloudy"),
            45 or 48 => ("Sisli", "foggy_morning"),
            51 or 53 or 55 => ("Hafif Çisenti", "rainy_window"),
            61 or 63 or 65 or 80 or 81 or 82 => ("Yağmurlu", "rainy_window"),
            71 or 73 or 75 or 77 or 85 or 86 => ("Karlı", "snowy_cabin"),
            95 or 96 or 99 => ("Fırtınalı / Şimşekli", "thunder_room"),
            _ => ("Huzurlu", "default_room")
        };
    }

    private static WeatherReportDto GetDefaultReport()
    {
        return new WeatherReportDto(
            21.5,
            8.2,
            1,
            "Parçalı Bulutlu",
            "cozy_cloudy",
            true,
            DateTime.UtcNow);
    }
}
