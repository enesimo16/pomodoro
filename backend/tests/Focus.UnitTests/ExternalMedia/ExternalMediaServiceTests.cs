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

    private static IMemoryCache NewCache() => new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public async Task PixabayVideoService_WhenApiKeyMissing_ReturnsEmpty()
    {
        var options = Options.Create(new ExternalApiSettings
        {
            Pixabay = new PixabaySettings { ApiKey = "" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new PixabayVideoService(httpClient, options, NewCache(), NullLogger<PixabayVideoService>.Instance);

        var videos = await service.SearchVideosAsync("rain", perPage: 4, page: 1);

        videos.Should().BeEmpty();
    }

    [Fact]
    public async Task PixabayVideoService_WhenApiReturnsError_ReturnsEmptyAndDoesNotCache()
    {
        var calls = 0;
        var options = Options.Create(new ExternalApiSettings
        {
            Pixabay = new PixabaySettings { ApiKey = "test-key" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        }));
        var service = new PixabayVideoService(httpClient, options, NewCache(), NullLogger<PixabayVideoService>.Instance);

        var first = await service.SearchVideosAsync("cyberpunk", perPage: 3, page: 2);
        var second = await service.SearchVideosAsync("cyberpunk", perPage: 3, page: 2);

        first.Should().BeEmpty();
        second.Should().BeEmpty();
        calls.Should().Be(2);
    }

    [Fact]
    public async Task PixabayVideoService_WhenApiReturnsEmptyHits_ReturnsEmptyAndCaches()
    {
        var calls = 0;
        var options = Options.Create(new ExternalApiSettings
        {
            Pixabay = new PixabaySettings { ApiKey = "test-key" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"hits\":[]}", Encoding.UTF8, "application/json")
            };
        }));
        var service = new PixabayVideoService(httpClient, options, NewCache(), NullLogger<PixabayVideoService>.Instance);

        var first = await service.SearchVideosAsync("zzz", perPage: 3, page: 1);
        var second = await service.SearchVideosAsync("zzz", perPage: 3, page: 1);

        first.Should().BeEmpty();
        second.Should().BeEmpty();
        calls.Should().Be(1);
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
        var service = new PixabayVideoService(httpClient, options, NewCache(), NullLogger<PixabayVideoService>.Instance);

        var videos = await service.SearchVideosAsync("rain", perPage: 5, page: 1);

        videos.Should().ContainSingle();
        videos[0].Id.Should().Be(99901);
        videos[0].MediumVideoUrl.Should().Be("https://cdn.example.com/med.mp4");
        videos[0].LargeVideoUrl.Should().Be("https://cdn.example.com/lrg.mp4");
    }

    [Fact]
    public async Task JamendoMusicService_WhenClientIdMissing_ReturnsEmpty()
    {
        var options = Options.Create(new ExternalApiSettings
        {
            Jamendo = new JamendoSettings { ClientId = "" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new JamendoMusicService(httpClient, options, NewCache(), NullLogger<JamendoMusicService>.Instance);

        var tracks = await service.SearchTracksAsync("lofi", limit: 4, page: 1);

        tracks.Should().BeEmpty();
    }

    [Fact]
    public async Task JamendoMusicService_WhenApiReturnsError_ReturnsEmpty()
    {
        var options = Options.Create(new ExternalApiSettings
        {
            Jamendo = new JamendoSettings { ClientId = "test-client" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)));
        var service = new JamendoMusicService(httpClient, options, NewCache(), NullLogger<JamendoMusicService>.Instance);

        var tracks = await service.SearchTracksAsync("Synth", limit: 5, page: 1);

        tracks.Should().BeEmpty();
    }

    [Fact]
    public async Task JamendoMusicService_WhenHeadersStatusFailed_ReturnsEmptyAndDoesNotCache()
    {
        var calls = 0;
        var json = """{"headers":{"status":"failed","error_message":"invalid client"},"results":[]}""";
        var options = Options.Create(new ExternalApiSettings
        {
            Jamendo = new JamendoSettings { ClientId = "bad-client" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }));
        var service = new JamendoMusicService(httpClient, options, NewCache(), NullLogger<JamendoMusicService>.Instance);

        await service.SearchTracksAsync("lofi", limit: 5, page: 1);
        var tracks = await service.SearchTracksAsync("lofi", limit: 5, page: 1);

        tracks.Should().BeEmpty();
        calls.Should().Be(2);
    }

    [Fact]
    public async Task JamendoMusicService_WhenApiReturnsTracks_ParsesAndCaches()
    {
        var calls = 0;
        var json = """
        {"headers":{"status":"success"},"results":[
          {"id":"7","name":"Night Flow","duration":120,"artist_name":"Artist","audio":"https://cdn.example.com/a.mp3","license_ccurl":"https://creativecommons.org/licenses/by/4.0/"}
        ]}
        """;
        var options = Options.Create(new ExternalApiSettings
        {
            Jamendo = new JamendoSettings { ClientId = "client" }
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }));
        var service = new JamendoMusicService(httpClient, options, NewCache(), NullLogger<JamendoMusicService>.Instance);

        var first = await service.SearchTracksAsync("lofi", limit: 5, page: 1);
        var second = await service.SearchTracksAsync("LOFI", limit: 5, page: 1);

        first.Should().ContainSingle().Which.Id.Should().Be("7");
        second.Should().HaveCount(1);
        calls.Should().Be(1);
    }
}
