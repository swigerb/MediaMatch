using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Enums;

namespace MediaMatch.Application.Tests.Services;

public sealed class MediaAnalysisServiceTests
{
    private readonly MediaAnalysisService _sut = new();

    [Fact]
    public async Task AnalyzeAsync_TvEpisode_DetectsCorrectly()
    {
        var result = await _sut.AnalyzeAsync("Breaking.Bad.S01E02.720p.BluRay.x264-DEMAND.mkv");

        result.MediaType.Should().Be(MediaType.TvSeries);
        result.Season.Should().Be(1);
        result.Episode.Should().Be(2);
        result.CleanTitle.Should().Be("Breaking Bad");
        result.VideoQuality.Should().Be("HD720p");
    }

    [Fact]
    public async Task AnalyzeAsync_Movie_DetectsCorrectly()
    {
        var result = await _sut.AnalyzeAsync("Inception.2010.1080p.BluRay.x264.mkv");

        result.MediaType.Should().Be(MediaType.Movie);
        result.Year.Should().Be(2010);
        result.CleanTitle.Should().Be("Inception");
        result.VideoQuality.Should().Be("HD1080p");
    }

    [Fact]
    public async Task AnalyzeAsync_Anime_DetectsCorrectly()
    {
        var result = await _sut.AnalyzeAsync("[SubGroup] Attack on Titan - 01 [1080p].mkv");

        result.MediaType.Should().Be(MediaType.Anime);
        result.Confidence.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AnalyzeAsync_Music_DetectsCorrectly()
    {
        var result = await _sut.AnalyzeAsync("Artist - Song Title.mp3");

        result.MediaType.Should().Be(MediaType.Music);
    }

    [Fact]
    public async Task AnalyzeAsync_Subtitle_DetectsCorrectly()
    {
        var result = await _sut.AnalyzeAsync("Movie.Name.2020.eng.srt");

        result.MediaType.Should().Be(MediaType.Subtitle);
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyPath_ThrowsArgumentException()
    {
        var act = () => _sut.AnalyzeAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AnalyzeAsync_NullPath_ThrowsArgumentException()
    {
        var act = () => _sut.AnalyzeAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AnalyzeBatchAsync_MultipleFiles_ReturnsCorrectCount()
    {
        var files = new[]
        {
            "Breaking.Bad.S01E01.mkv",
            "Inception.2010.mkv",
            "song.mp3"
        };

        var results = await _sut.AnalyzeBatchAsync(files);

        results.Should().HaveCount(3);
        results[0].MediaType.Should().Be(MediaType.TvSeries);
        results[1].MediaType.Should().Be(MediaType.Movie);
        results[2].MediaType.Should().Be(MediaType.Music);
    }

    [Fact]
    public async Task AnalyzeBatchAsync_EmptyList_ReturnsEmptyResults()
    {
        var results = await _sut.AnalyzeBatchAsync([]);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_UnicodeFileName_HandlesCorrectly()
    {
        var result = await _sut.AnalyzeAsync("日本語タイトル.S01E01.mkv");

        result.Should().NotBeNull();
        result.MediaType.Should().Be(MediaType.TvSeries);
        result.Season.Should().Be(1);
        result.Episode.Should().Be(1);
    }

    [Fact]
    public async Task AnalyzeAsync_DirectoryWithSeasonFolder_ExtractsTitleFromParent()
    {
        // Uses path separator for directory structure
        var path = Path.Combine("TV Shows", "Breaking Bad", "Season 01", "episode.S01E01.mkv");

        var result = await _sut.AnalyzeAsync(path);

        result.Should().NotBeNull();
        result.MediaType.Should().Be(MediaType.TvSeries);
    }

    [Fact]
    public async Task AnalyzeBatchAsync_Cancellation_ThrowsOperationCancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _sut.AnalyzeBatchAsync(["file.mkv"], cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AnalyzeAsync_VeryLongPath_HandlesGracefully()
    {
        var longName = new string('a', 200) + ".S01E01.mkv";
        var result = await _sut.AnalyzeAsync(longName);

        result.Should().NotBeNull();
        result.FilePath.Should().Be(longName);
    }

    [Fact]
    public async Task AnalyzeAsync_UnknownExtension_ReturnsUnknown()
    {
        var result = await _sut.AnalyzeAsync("document.pdf");

        result.MediaType.Should().Be(MediaType.Unknown);
    }

    [Fact]
    public async Task AnalyzeAsync_ReleaseGroup_ExtractsCorrectly()
    {
        var result = await _sut.AnalyzeAsync("Movie.Name.2020.1080p.BluRay-GROUP.mkv");

        result.ReleaseGroup.Should().Be("GROUP");
        result.VideoSource.Should().Be("BluRay");
    }
}
