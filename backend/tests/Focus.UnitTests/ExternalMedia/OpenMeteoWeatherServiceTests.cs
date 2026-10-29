using System.Net;
using System.Text;
using FluentAssertions;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Focus.UnitTests.ExternalMedia;

public class OpenMeteoWeatherServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private static OpenMeteoWeatherService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        return new OpenMeteoWeatherService(
            new HttpClient(new StubHandler(responder)),
            Options.Create(new ExternalApiSettings()),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<OpenMeteoWeatherService>.Instance);
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task GetCurrentWeather_WhenApiFails_ReturnsNull()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));

        var report = await service.GetCurrentWeatherAsync(41.01, 28.97);

        report.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentWeather_WhenPayloadHasNoCurrentWeather_ReturnsNull()
    {
        var service = CreateService(_ => Json("{}"));

        var report = await service.GetCurrentWeatherAsync(41.01, 28.97);

        report.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentWeather_ParsesReportAndTreatsTimeAsUtc()
    {
        var service = CreateService(_ => Json("""{"current_weather":{"temperature":12.5,"windspeed":7.0,"weathercode":61,"is_day":0,"time":"2026-10-29T08:00"}}"""));

        var report = await service.GetCurrentWeatherAsync(41.01, 28.97);

        report.Should().NotBeNull();
        report!.WeatherCode.Should().Be(61);
        report.Temperature.Should().Be(12.5);
        report.IsDay.Should().BeFalse();
        report.Time.Should().Be(new DateTime(2026, 10, 29, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetCurrentWeather_CachesPerRoundedCoordinates()
    {
        var calls = 0;
        var service = CreateService(_ =>
        {
            calls++;
            return Json("""{"current_weather":{"temperature":10,"windspeed":1,"weathercode":0,"is_day":1,"time":"2026-10-29T08:00"}}""");
        });

        await service.GetCurrentWeatherAsync(41.0082, 28.9784);
        await service.GetCurrentWeatherAsync(41.0112, 28.9751);
        await service.GetCurrentWeatherAsync(48.85, 2.35);

        calls.Should().Be(2);
    }
}
