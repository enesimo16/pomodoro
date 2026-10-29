using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.ExternalMedia.DTOs;
using Focus.IntegrationTests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Focus.IntegrationTests.ExternalMedia;

public class ExternalMediaApiTests : IClassFixture<CustomWebApplicationFactory>, IClassFixture<ProductionWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ProductionWebApplicationFactory _productionFactory;

    public ExternalMediaApiTests(CustomWebApplicationFactory factory, ProductionWebApplicationFactory productionFactory)
    {
        _factory = factory;
        _productionFactory = productionFactory;
    }

    private sealed class FakeWeatherService : IWeatherService
    {
        public int? LastSimulatedCode { get; private set; }
        public WeatherReportDto? Report { get; set; } = new(
            12.0, 3.0, 61, "Sağanak Yağmurlu", "rainy_window", "rain_drops", "Yağmur", "#0984E3", "Yağmur", true, DateTime.UtcNow);

        public Task<WeatherReportDto?> GetCurrentWeatherAsync(
            double latitude = 41.0082,
            double longitude = 28.9784,
            int? simulatedWeatherCode = null,
            CancellationToken cancellationToken = default)
        {
            LastSimulatedCode = simulatedWeatherCode;
            return Task.FromResult(Report);
        }
    }

    private sealed class FakeVideoService : IThemeVideoService
    {
        public Task<IReadOnlyList<ThemeVideoDto>> SearchVideosAsync(string query = "rain window", int perPage = 12, int page = 1, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ThemeVideoDto>>([new ThemeVideoDto(1, "https://pixabay.com/videos/id-1/", 10, "t", "m", "l", "rain")]);
    }

    private sealed class FakeMusicService : IMusicTrackService
    {
        public Task<IReadOnlyList<MusicTrackDto>> SearchTracksAsync(string query = "lofi", int limit = 12, int page = 1, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MusicTrackDto>>([new MusicTrackDto("1", "Track", 100, "Artist", "https://cdn.example.com/a.mp3", null, null)]);

        public Task<IReadOnlyList<MusicTrackDto>> GetLofiTracksAsync(int limit = 12, int page = 1, CancellationToken cancellationToken = default)
            => SearchTracksAsync("lofi", limit, page, cancellationToken);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, FakeWeatherService weather)
    {
        return factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWeatherService>();
            services.RemoveAll<IThemeVideoService>();
            services.RemoveAll<IMusicTrackService>();
            services.AddSingleton<IWeatherService>(weather);
            services.AddSingleton<IThemeVideoService, FakeVideoService>();
            services.AddSingleton<IMusicTrackService, FakeMusicService>();
        })).CreateClient();
    }

    [Fact]
    public async Task GetThemeVideosAndTracks_ReturnOk()
    {
        var client = CreateClient(_factory, new FakeWeatherService());

        var videos = await client.GetAsync("/api/v1/media/themes?query=rain&perPage=4&page=1");
        var tracks = await client.GetAsync("/api/v1/media/tracks?query=lofi&limit=4&page=1");

        videos.StatusCode.Should().Be(HttpStatusCode.OK);
        tracks.StatusCode.Should().Be(HttpStatusCode.OK);
        (await videos.Content.ReadFromJsonAsync<List<ThemeVideoDto>>()).Should().NotBeNullOrEmpty();
        (await tracks.Content.ReadFromJsonAsync<List<MusicTrackDto>>()).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetThemeVideos_WithOverlongQuery_ReturnsBadRequest()
    {
        var client = CreateClient(_factory, new FakeWeatherService());

        var response = await client.GetAsync($"/api/v1/media/themes?query={new string('a', 101)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWeather_ReturnsOkAndWeatherData()
    {
        var client = CreateClient(_factory, new FakeWeatherService());

        var response = await client.GetAsync("/api/v1/media/weather?latitude=41.0082&longitude=28.9784");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var weather = await response.Content.ReadFromJsonAsync<WeatherReportDto>();
        weather!.WeatherCode.Should().Be(61);
    }

    [Theory]
    [InlineData("91", "10")]
    [InlineData("-91", "10")]
    [InlineData("10", "181")]
    [InlineData("10", "-181")]
    public async Task GetWeather_WithOutOfRangeCoordinates_ReturnsBadRequest(string latitude, string longitude)
    {
        var client = CreateClient(_factory, new FakeWeatherService());

        var response = await client.GetAsync($"/api/v1/media/weather?latitude={latitude}&longitude={longitude}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWeather_WhenProviderUnavailable_ReturnsServiceUnavailable()
    {
        var client = CreateClient(_factory, new FakeWeatherService { Report = null });

        var response = await client.GetAsync("/api/v1/media/weather?latitude=41&longitude=29");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetWeather_SimulatedCodeIsHonoredOnlyInDevelopment()
    {
        var devWeather = new FakeWeatherService();
        var prodWeather = new FakeWeatherService();

        await CreateClient(_factory, devWeather).GetAsync("/api/v1/media/weather?weatherCode=95");
        await CreateClient(_productionFactory, prodWeather).GetAsync("/api/v1/media/weather?weatherCode=95");

        devWeather.LastSimulatedCode.Should().Be(95);
        prodWeather.LastSimulatedCode.Should().BeNull();
    }
}
