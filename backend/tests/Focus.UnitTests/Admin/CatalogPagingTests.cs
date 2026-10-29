using Focus.Application.Common.Interfaces;
using Focus.Application.Features.ExternalMedia.DTOs;
using Focus.UnitTests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using MusicModel = Focus.Admin.Pages.Music.IndexModel;
using ThemesModel = Focus.Admin.Pages.Themes.IndexModel;

namespace Focus.UnitTests.Admin;

public class CatalogPagingTests
{
    private static IReadOnlyList<ThemeVideoDto> Videos(int count) =>
        Enumerable.Range(1, count).Select(i => new ThemeVideoDto(i, "page", 10, "thumb", "medium", "large", "rain, window")).ToList();

    private static IReadOnlyList<MusicTrackDto> Tracks(int count) =>
        Enumerable.Range(1, count).Select(i => new MusicTrackDto($"{i}", $"Track {i}", 120, "Artist", "https://audio", null, null)).ToList();

    private static (ThemesModel Page, Mock<IThemeVideoService> Service) CreateThemes(int videoCount)
    {
        var service = new Mock<IThemeVideoService>();
        service.Setup(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Videos(videoCount));
        var page = new ThemesModel(service.Object, TestDbContextFactory.Create(), NullLogger<ThemesModel>.Instance);
        return (page, service);
    }

    [Fact]
    public async Task Themes_FullExternalPage_HasNextPage()
    {
        var (page, _) = CreateThemes(12);

        await page.OnGetAsync(CancellationToken.None);

        Assert.True(page.HasNextPage);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task Themes_PartialExternalPage_IsLastPage()
    {
        var (page, _) = CreateThemes(5);

        await page.OnGetAsync(CancellationToken.None);

        Assert.False(page.HasNextPage);
        Assert.Equal(1, page.TotalPages);
    }

    [Fact]
    public async Task Themes_AiPresetsAppearOnlyOnFirstPage()
    {
        var (first, _) = CreateThemes(0);
        await first.OnGetAsync(CancellationToken.None);

        var (second, _) = CreateThemes(0);
        second.PageNumber = 2;
        await second.OnGetAsync(CancellationToken.None);

        Assert.Equal(7, first.AiPresetCount);
        Assert.Equal(0, second.AiPresetCount);
    }

    [Theory]
    [InlineData(-3, 1)]
    [InlineData(0, 1)]
    [InlineData(int.MaxValue, ThemesModel.MaxPageNumber)]
    public async Task Themes_PageNumberIsClamped(int requested, int expected)
    {
        var (page, _) = CreateThemes(0);
        page.PageNumber = requested;

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(expected, page.PageNumber);
    }

    [Fact]
    public async Task Themes_AiOnlyFilter_DoesNotCallExternalService()
    {
        var (page, service) = CreateThemes(12);
        page.SourceFilter = "Yapay Zeka";

        await page.OnGetAsync(CancellationToken.None);

        service.Verify(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public async Task Themes_ExternalFailure_StillRendersAiPresets()
    {
        var service = new Mock<IThemeVideoService>();
        service.Setup(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("down"));
        var page = new ThemesModel(service.Object, TestDbContextFactory.Create(), NullLogger<ThemesModel>.Instance);

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(7, page.AiPresetCount);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public async Task Music_FullExternalPage_HasNextPageAndStaticItemsOnlyOnFirstPage()
    {
        var service = new Mock<IMusicTrackService>();
        service.Setup(s => s.SearchTracksAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tracks(12));

        var first = new MusicModel(service.Object, NullLogger<MusicModel>.Instance);
        await first.OnGetAsync(CancellationToken.None);

        var second = new MusicModel(service.Object, NullLogger<MusicModel>.Instance) { PageNumber = 2 };
        await second.OnGetAsync(CancellationToken.None);

        Assert.True(first.HasNextPage);
        Assert.True(first.AiMixerCount > 0);
        Assert.True(first.AmbientNoiseCount > 0);
        Assert.Equal(0, second.AiMixerCount);
        Assert.Equal(0, second.AmbientNoiseCount);
        Assert.Equal(12, second.JamendoCount);
    }

    [Fact]
    public async Task Music_PartialExternalPage_IsLastPage()
    {
        var service = new Mock<IMusicTrackService>();
        service.Setup(s => s.SearchTracksAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tracks(3));

        var page = new MusicModel(service.Object, NullLogger<MusicModel>.Instance);
        await page.OnGetAsync(CancellationToken.None);

        Assert.False(page.HasNextPage);
        Assert.Equal(1, page.TotalPages);
    }
}
