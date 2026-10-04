using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.ExternalMedia.DTOs;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.ExternalMedia;

public class ExternalMediaApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ExternalMediaApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetThemeVideos_ReturnsOkAndThemeList()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/media/themes?query=rain&perPage=4");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var videos = await response.Content.ReadFromJsonAsync<List<ThemeVideoDto>>();
        videos.Should().NotBeNull();
        videos!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLofiTracks_ReturnsOkAndTracksList()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/media/tracks?limit=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tracks = await response.Content.ReadFromJsonAsync<List<MusicTrackDto>>();
        tracks.Should().NotBeNull();
        tracks!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWeather_ReturnsOkAndWeatherData()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/media/weather?latitude=41.0082&longitude=28.9784");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var weather = await response.Content.ReadFromJsonAsync<WeatherReportDto>();
        weather.Should().NotBeNull();
        weather!.WeatherCondition.Should().NotBeNullOrWhiteSpace();
        weather.SuggestedTheme.Should().NotBeNullOrWhiteSpace();
    }
}
