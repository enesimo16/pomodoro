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
    public async Task GetThemeVideos_ReturnsOkAndSupportsPagination()
    {
        var client = _factory.CreateClient();

        var responsePage1 = await client.GetAsync("/api/v1/media/themes?query=rain&perPage=4&page=1");
        var responsePage2 = await client.GetAsync("/api/v1/media/themes?query=rain&perPage=4&page=2");

        responsePage1.StatusCode.Should().Be(HttpStatusCode.OK);
        responsePage2.StatusCode.Should().Be(HttpStatusCode.OK);

        var videosPage1 = await responsePage1.Content.ReadFromJsonAsync<List<ThemeVideoDto>>();
        var videosPage2 = await responsePage2.Content.ReadFromJsonAsync<List<ThemeVideoDto>>();

        videosPage1.Should().NotBeNull().And.NotBeEmpty();
        videosPage2.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GetLofiTracks_ReturnsOkAndSupportsQueryAndPagination()
    {
        var client = _factory.CreateClient();

        var responsePage1 = await client.GetAsync("/api/v1/media/tracks?query=lofi&limit=4&page=1");
        var responsePage2 = await client.GetAsync("/api/v1/media/tracks?query=lofi&limit=4&page=2");

        responsePage1.StatusCode.Should().Be(HttpStatusCode.OK);
        responsePage2.StatusCode.Should().Be(HttpStatusCode.OK);

        var tracksPage1 = await responsePage1.Content.ReadFromJsonAsync<List<MusicTrackDto>>();
        var tracksPage2 = await responsePage2.Content.ReadFromJsonAsync<List<MusicTrackDto>>();

        tracksPage1.Should().NotBeNull().And.NotBeEmpty();
        tracksPage2.Should().NotBeNull().And.NotBeEmpty();
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
