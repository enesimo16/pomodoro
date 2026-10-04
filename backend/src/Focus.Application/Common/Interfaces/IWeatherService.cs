using Focus.Application.Features.ExternalMedia.DTOs;

namespace Focus.Application.Common.Interfaces;

public interface IWeatherService
{
    Task<WeatherReportDto?> GetCurrentWeatherAsync(double latitude = 41.0082, double longitude = 28.9784, CancellationToken cancellationToken = default);
}
