using MediaMatch.Application.Detection;
using MediaMatch.Application.Expressions;
using MediaMatch.Application.Matching;
using MediaMatch.Application.Pipeline;
using MediaMatch.Application.Services;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using Moq;

namespace MediaMatch.EndToEnd.Tests.Fixtures;

/// <summary>
/// Shared fixture for E2E tests that wires together all real application-layer
/// components with only external dependencies (providers, file system) mocked.
/// </summary>
public sealed class MediaMatchFixture : IDisposable
{
    public readonly MediaDetector Detector = new();
    public readonly ReleaseInfoParser ReleaseParser = new();
    public readonly EpisodeMatcher EpisodeMatcher = new();
    public readonly ScribanExpressionEngine ExpressionEngine = new();
    public readonly Mock<IEpisodeProvider> EpisodeProvider = new();
    public readonly Mock<IMovieProvider> MovieProvider = new();
    public readonly Mock<IFileSystem> FileSystem = new();

    public MatchingPipeline CreatePipeline(
        IEpisodeProvider[]? extraEpisodeProviders = null,
        IMovieProvider[]? extraMovieProviders = null)
    {
        var episodeProviders = new List<IEpisodeProvider> { EpisodeProvider.Object };
        if (extraEpisodeProviders is not null)
            episodeProviders.AddRange(extraEpisodeProviders);

        var movieProviders = new List<IMovieProvider> { MovieProvider.Object };
        if (extraMovieProviders is not null)
            movieProviders.AddRange(extraMovieProviders);

        return new MatchingPipeline(
            Detector, ReleaseParser, EpisodeMatcher,
            episodeProviders, movieProviders);
    }

    public RenamePreviewService CreatePreviewService(MatchingPipeline? pipeline = null) =>
        new(pipeline ?? CreatePipeline(), ExpressionEngine);

    public FileOrganizationService CreateOrganizationService(RenamePreviewService? previewService = null) =>
        new(previewService ?? CreatePreviewService(), FileSystem.Object);

    public void SetupEpisodeProvider(
        string seriesName,
        int seriesId,
        IReadOnlyList<Episode> episodes,
        SeriesInfo? seriesInfo = null)
    {
        EpisodeProvider.Setup(p => p.Name).Returns("MockEpisodeProvider");
        EpisodeProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult> { new(seriesName, seriesId) });
        EpisodeProvider
            .Setup(p => p.GetEpisodesAsync(It.IsAny<SearchResult>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        EpisodeProvider
            .Setup(p => p.GetSeriesInfoAsync(It.IsAny<SearchResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(seriesInfo ?? new SeriesInfo(seriesName, seriesId.ToString(), null, null, null, null, null, []));
    }

    public void SetupMovieProvider(string movieName, int year, int tmdbId)
    {
        MovieProvider.Setup(p => p.Name).Returns("MockMovieProvider");
        MovieProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { new(movieName, year, TmdbId: tmdbId) });
        MovieProvider
            .Setup(p => p.GetMovieInfoAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieInfo(movieName, year, tmdbId, null, null, null, null, null, null, null, [], [], []));
    }

    public void SetupEmptyProviders()
    {
        EpisodeProvider.Setup(p => p.Name).Returns("EmptyEpisodeProvider");
        EpisodeProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>());

        MovieProvider.Setup(p => p.Name).Returns("EmptyMovieProvider");
        MovieProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie>());
    }

    public void Dispose() { }
}

/// <summary>
/// Disposable temp directory fixture for tests that need real files on disk.
/// </summary>
public sealed class TempDirectoryFixture : IDisposable
{
    public string RootPath { get; } = Directory.CreateTempSubdirectory("mediamatch_e2e_").FullName;

    /// <summary>Creates an empty file at the given relative path and returns the full path.</summary>
    public string CreateFile(string relativePath)
    {
        var fullPath = Path.Combine(RootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, string.Empty);
        return fullPath;
    }

    /// <summary>Creates a subdirectory and returns its full path.</summary>
    public string CreateDirectory(string relativePath)
    {
        var fullPath = Path.Combine(RootPath, relativePath);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public void Dispose()
    {
        try { Directory.Delete(RootPath, recursive: true); }
        catch { /* best effort */ }
    }
}
