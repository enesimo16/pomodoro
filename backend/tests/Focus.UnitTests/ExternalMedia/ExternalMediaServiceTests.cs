using System.Net;
using System.Text;
using FluentAssertions;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Focus.UnitTests.ExternalMedia;

public class ExternalMediaServiceTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    [Fact]
    public async Task PixabayVideoService_WhenApiKeyMissing_ReturnsPaginatedFallbacks()
    {
        var options = Options.Create(new ExternalApiSettings
        {
            Pixabay = new PixabaySettings { ApiKey = "" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new PixabayVideoService(httpClient, options, NullLogger<PixabayVideoService>.Instance);

        var page1 = await service.SearchVideosAsync("", perPage: 4, page: 1);
        var page2 = await service.SearchVideosAsync("", perPage: 4, page: 2);
        var pageHigh = await service.SearchVideosAsync("", perPage: 4, page: 10);

        page1.Should().HaveCount(4);
        page2.Should().HaveCount(4);
        pageHigh.Should().HaveCount(4);
        page1.Select(v => v.Id).Should().NotEqual(page2.Select(v => v.Id));
    }

    [Fact]
    public async Task PixabayVideoService_WhenApiReturnsError_FallsBackWithPagination()
    {
        var options = Options.Create(new ExternalApiSettings
        {
            Pixabay = new PixabaySettings { ApiKey = "test-key" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        var service = new PixabayVideoService(httpClient, options, NullLogger<PixabayVideoService>.Instance);

        var videos = await service.SearchVideosAsync("cyberpunk", perPage: 3, page: 2);

        videos.Should().NotBeEmpty();
        videos.Should().Contain(v => v.Tags.Contains("cyberpunk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PixabayVideoService_WhenApiReturnsValidJson_ParsesVideoDtos()
    {
        var json = """
        {
          "hits": [
            {
              "id": 99901,
              "pageURL": "https://pixabay.com/videos/id-99901/",
              "duration": 42,
              "tags": "rain, lofi, window",
              "picture_id": "pic999",
              "videos": {
                "medium": { "url": "https://cdn.example.com/med.mp4" },
                "large": { "url": "https://cdn.example.com/lrg.mp4" }
              }
            }
          ]
        }
        """;

        var options = Options.Create(new ExternalApiSettings
        {
            Pixabay = new PixabaySettings { ApiKey = "valid-key" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }));
        var service = new PixabayVideoService(httpClient, options, NullLogger<PixabayVideoService>.Instance);

        var videos = await service.SearchVideosAsync("rain", perPage: 5, page: 1);

        videos.Should().ContainSingle();
        videos[0].Id.Should().Be(99901);
        videos[0].MediumVideoUrl.Should().Be("https://cdn.example.com/med.mp4");
        videos[0].LargeVideoUrl.Should().Be("https://cdn.example.com/lrg.mp4");
    }

    [Fact]
    public async Task JamendoMusicService_WhenClientIdMissing_ReturnsPaginatedFallbacks()
    {
        var options = Options.Create(new ExternalApiSettings
        {
            Jamendo = new JamendoSettings { ClientId = "" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new JamendoMusicService(httpClient, options, NullLogger<JamendoMusicService>.Instance);

        var page1 = await service.SearchTracksAsync("lofi", limit: 4, page: 1);
        var page2 = await service.SearchTracksAsync("lofi", limit: 4, page: 2);
        var pageHigh = await service.SearchTracksAsync("lofi", limit: 4, page: 8);

        page1.Should().HaveCount(4);
        page2.Should().HaveCount(4);
        pageHigh.Should().HaveCount(4);
        page1.Select(t => t.Id).Should().NotEqual(page2.Select(t => t.Id));
    }

    [Fact]
    public async Task JamendoMusicService_WhenApiReturnsError_FallsBackWithQueryFilter()
    {
        var options = Options.Create(new ExternalApiSettings
        {
            Jamendo = new JamendoSettings { ClientId = "test-client" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)));
        var service = new JamendoMusicService(httpClient, options, NullLogger<JamendoMusicService>.Instance);

        var tracks = await service.SearchTracksAsync("Synth", limit: 5, page: 1);

        tracks.Should().NotBeEmpty();
        tracks.Should().Contain(t => t.Name.Contains("Synth", StringComparison.OrdinalIgnoreCase));
    }
}
