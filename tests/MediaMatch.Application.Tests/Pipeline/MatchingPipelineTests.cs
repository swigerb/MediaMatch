using FluentAssertions;
using MediaMatch.Application.Pipeline;
using MediaMatch.Application.Detection;
using MediaMatch.Application.Matching;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using Moq;

namespace MediaMatch.Application.Tests.Pipeline;

public class MatchingPipelineTests
{
    private readonly Mock<IEpisodeProvider> _episodeProvider = new();
    private readonly Mock<IMovieProvider> _movieProvider = new();

    private MatchingPipeline CreatePipeline() =>
        new([_episodeProvider.Object], [_movieProvider.Object]);

    [Fact]
    public async Task ProcessAsync_TvEpisode_MatchesViaEpisodeProvider()
    {
        var searchResults = new List<SearchResult>
        {
            new("Breaking Bad", 1)
        };

        var episodes = new List<Episode>
        {
            new("Breaking Bad", 1, 2, "Cat's in the Bag...")
        };

        var seriesInfo = new SeriesInfo(
            "Breaking Bad", "1", "A chemistry teacher...", "AMC", "Ended",
            9.5, 47, ["Drama", "Thriller"]);

        _episodeProvider.Setup(p => p.Name).Returns("TestProvider");
        _episodeProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);
        _episodeProvider
            .Setup(p => p.GetEpisodesAsync(It.IsAny<SearchResult>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _episodeProvider
            .Setup(p => p.GetSeriesInfoAsync(It.IsAny<SearchResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(seriesInfo);

        var pipeline = CreatePipeline();
        var result = await pipeline.ProcessAsync("Breaking.Bad.S01E02.720p.mkv");

        result.IsMatch.Should().BeTrue();
        result.MediaType.Should().Be(MediaType.TvSeries);
        result.Episode.Should().NotBeNull();
        result.Episode!.SeriesName.Should().Be("Breaking Bad");
        result.ProviderSource.Should().Be("TestProvider");
    }

    [Fact]
    public async Task ProcessAsync_Movie_MatchesViaMovieProvider()
    {
        var movies = new List<Movie>
        {
            new("Inception", 2010, TmdbId: 27205)
        };

        var movieInfo = new MovieInfo(
            "Inception", 2010, 27205, "tt1375666",
            "A thief who steals...", "Your mind is the scene of the crime.",
            null, 8.8, 148, "PG-13",
            ["Action", "Sci-Fi"], [], []);

        _movieProvider.Setup(p => p.Name).Returns("TestMovieProvider");
        _movieProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);
        _movieProvider
            .Setup(p => p.GetMovieInfoAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(movieInfo);

        var pipeline = CreatePipeline();
        var result = await pipeline.ProcessAsync("Inception.2010.1080p.BluRay.mkv");

        result.IsMatch.Should().BeTrue();
        result.MediaType.Should().Be(MediaType.Movie);
        result.Movie.Should().NotBeNull();
        result.Movie!.Name.Should().Be("Inception");
        result.ProviderSource.Should().Be("TestMovieProvider");
    }

    [Fact]
    public async Task ProcessAsync_UnknownType_ReturnsNoMatch()
    {
        var pipeline = CreatePipeline();
        var result = await pipeline.ProcessAsync("document.pdf");

        result.IsMatch.Should().BeFalse();
        result.MediaType.Should().Be(MediaType.Unknown);
    }

    [Fact]
    public async Task ProcessAsync_MusicFile_ReturnsNoMatch()
    {
        var pipeline = CreatePipeline();
        var result = await pipeline.ProcessAsync("song.mp3");

        result.IsMatch.Should().BeFalse();
        result.MediaType.Should().Be(MediaType.Music);
    }

    [Fact]
    public async Task ProcessAsync_NoProviderResults_ReturnsNoMatch()
    {
        _episodeProvider.Setup(p => p.Name).Returns("Empty");
        _episodeProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>());

        var pipeline = CreatePipeline();
        var result = await pipeline.ProcessAsync("Show.S01E01.mkv");

        result.IsMatch.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_ProviderThrows_ContinuesToNextProvider()
    {
        var failingProvider = new Mock<IEpisodeProvider>();
        failingProvider.Setup(p => p.Name).Returns("Failing");
        failingProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var workingProvider = new Mock<IEpisodeProvider>();
        workingProvider.Setup(p => p.Name).Returns("Working");
        workingProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult> { new("Show", 1) });
        workingProvider
            .Setup(p => p.GetEpisodesAsync(It.IsAny<SearchResult>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Episode> { new("Show", 1, 1, "Pilot") });
        workingProvider
            .Setup(p => p.GetSeriesInfoAsync(It.IsAny<SearchResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeriesInfo("Show", "1", null, null, null, null, null, []));

        var pipeline = new MatchingPipeline(
            [failingProvider.Object, workingProvider.Object],
            [_movieProvider.Object]);

        var result = await pipeline.ProcessAsync("Show.S01E01.mkv");

        result.IsMatch.Should().BeTrue();
        result.ProviderSource.Should().Be("Working");
    }

    [Fact]
    public async Task ProcessBatchAsync_MultipleFiles_ReturnsResultsForAll()
    {
        _episodeProvider.Setup(p => p.Name).Returns("Test");
        _episodeProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>());

        _movieProvider.Setup(p => p.Name).Returns("Test");
        _movieProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie>());

        var pipeline = CreatePipeline();
        var results = await pipeline.ProcessBatchAsync(["Show.S01E01.mkv", "Movie.2020.mkv"]);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessAsync_EmptyPath_ThrowsArgumentException()
    {
        var pipeline = CreatePipeline();
        var act = () => pipeline.ProcessAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessBatchAsync_Cancellation_Throws()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var pipeline = CreatePipeline();
        var act = () => pipeline.ProcessBatchAsync(["file.mkv"], cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
