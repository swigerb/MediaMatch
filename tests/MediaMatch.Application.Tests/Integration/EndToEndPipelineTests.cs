using FluentAssertions;
using MediaMatch.Application.Detection;
using MediaMatch.Application.Expressions;
using MediaMatch.Application.Matching;
using MediaMatch.Application.Pipeline;
using MediaMatch.Application.Services;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using Moq;

namespace MediaMatch.Application.Tests.Integration;

/// <summary>
/// Integration tests that exercise the full pipeline: file path → detection → matching → rename preview.
/// Real detection/matching/expression engines are used; only providers and file system are mocked.
/// </summary>
public sealed class EndToEndPipelineTests
{
    private readonly MediaDetector _detector = new();
    private readonly ReleaseInfoParser _releaseParser = new();
    private readonly EpisodeMatcher _episodeMatcher = new();
    private readonly ScribanExpressionEngine _expressionEngine = new();
    private readonly Mock<IEpisodeProvider> _episodeProvider = new();
    private readonly Mock<IMovieProvider> _movieProvider = new();
    private readonly Mock<IFileSystem> _fileSystem = new();

    private MatchingPipeline CreatePipeline() =>
        new(_detector, _releaseParser, _episodeMatcher,
            [_episodeProvider.Object], [_movieProvider.Object]);

    private RenamePreviewService CreatePreviewService(MatchingPipeline pipeline) =>
        new(pipeline, _expressionEngine);

    private FileOrganizationService CreateOrganizationService(RenamePreviewService previewService) =>
        new(previewService, _fileSystem.Object);

    private void SetupEpisodeProvider(
        string seriesName, int seriesId,
        IReadOnlyList<Episode> episodes,
        SeriesInfo? seriesInfo = null)
    {
        _episodeProvider.Setup(p => p.Name).Returns("MockProvider");
        _episodeProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult> { new(seriesName, seriesId) });
        _episodeProvider
            .Setup(p => p.GetEpisodesAsync(It.IsAny<SearchResult>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(episodes);
        _episodeProvider
            .Setup(p => p.GetSeriesInfoAsync(It.IsAny<SearchResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(seriesInfo ?? new SeriesInfo(seriesName, seriesId.ToString(), null, null, null, null, null, []));
    }

    private void SetupMovieProvider(string movieName, int year, int tmdbId)
    {
        _movieProvider.Setup(p => p.Name).Returns("MockMovieProvider");
        _movieProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { new(movieName, year, TmdbId: tmdbId) });
        _movieProvider
            .Setup(p => p.GetMovieInfoAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieInfo(
                movieName, year, tmdbId, null, null, null, null, null, null, null, [], [], []));
    }

    private void SetupEmptyProviders()
    {
        _episodeProvider.Setup(p => p.Name).Returns("EmptyProvider");
        _episodeProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>());

        _movieProvider.Setup(p => p.Name).Returns("EmptyMovieProvider");
        _movieProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie>());
    }

    // ── Test 1: TV Episode full pipeline ──────────────────────────────────

    [Fact]
    public async Task TvEpisode_FullPipeline_DetectsAndGeneratesPreview()
    {
        const string input = "Breaking.Bad.S01E02.720p.BluRay.mkv";

        SetupEpisodeProvider("Breaking Bad", 1, new List<Episode>
        {
            new("Breaking Bad", 1, 1, "Pilot"),
            new("Breaking Bad", 1, 2, "Cat's in the Bag..."),
            new("Breaking Bad", 1, 3, "...And the Bag's in the River"),
        });

        var pipeline = CreatePipeline();
        var previewService = CreatePreviewService(pipeline);
        var results = await previewService.PreviewAsync(
            [input], "{n} - {s00e00} - {t}");
        results.Should().HaveCount(1);
        var result = results[0];
        result.Success.Should().BeTrue();
        result.OriginalPath.Should().Be(input);
        result.NewPath.Should().Contain("Breaking Bad - S01E02 - Cat's in the Bag...");
        result.NewPath.Should().EndWith(".mkv");
        result.MatchConfidence.Should().BeGreaterThan(0);
        result.MediaType.Should().Be(MediaType.TvSeries);
    }

    // ── Test 2: Movie full pipeline ───────────────────────────────────────

    [Fact]
    public async Task Movie_FullPipeline_DetectsAndGeneratesPreview()
    {
        const string input = "Inception.2010.1080p.BluRay.mkv";

        SetupMovieProvider("Inception", 2010, 27205);

        var pipeline = CreatePipeline();
        var previewService = CreatePreviewService(pipeline);
        var results = await previewService.PreviewAsync(
            [input], "{n} ({y})");
        results.Should().HaveCount(1);
        var result = results[0];
        result.Success.Should().BeTrue();
        result.NewPath.Should().Contain("Inception (2010)");
        result.NewPath.Should().EndWith(".mkv");
        result.MediaType.Should().Be(MediaType.Movie);
    }

    // ── Test 3: Unrecognized file ─────────────────────────────────────────

    [Fact]
    public async Task UnrecognizedFile_FullPipeline_ReturnsNoMatch()
    {
        const string input = "random_file.txt";
        SetupEmptyProviders();

        var pipeline = CreatePipeline();
        var previewService = CreatePreviewService(pipeline);
        var results = await previewService.PreviewAsync([input], "{n}");
        results.Should().HaveCount(1);
        var result = results[0];
        result.Success.Should().BeFalse();
    }

    // ── Test 4: Multiple files batch ──────────────────────────────────────

    [Fact]
    public async Task MultipleFiles_BatchPipeline_ProcessesAll()
    {
        SetupEpisodeProvider("Breaking Bad", 1, new List<Episode>
        {
            new("Breaking Bad", 1, 1, "Pilot"),
            new("Breaking Bad", 1, 2, "Cat's in the Bag..."),
        });

        SetupMovieProvider("Inception", 2010, 27205);

        var pipeline = CreatePipeline();
        var previewService = CreatePreviewService(pipeline);

        var files = new[]
        {
            "Breaking.Bad.S01E01.mkv",
            "Breaking.Bad.S01E02.mkv",
            "Inception.2010.mkv",
        };
        var results = await previewService.PreviewAsync(files, "{n}");
        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.Success);
    }

    // ── Test 5: Unicode file names ────────────────────────────────────────

    [Theory]
    [InlineData("Über.Cool.Show.S02E03.mkv")]
    [InlineData("日本語アニメ.S01E01.mkv")]
    [InlineData("Señor.de.los.Anillos.S01E01.mkv")]
    public async Task UnicodeFileName_Pipeline_HandlesCorrectly(string input)
    {
        // Arrange — provider may or may not match; the key assertion is no crash
        SetupEmptyProviders();

        var pipeline = CreatePipeline();
        var previewService = CreatePreviewService(pipeline);
        var act = () => previewService.PreviewAsync([input], "{n}");

        // Assert — should not throw
        var results = await act.Should().NotThrowAsync();
        results.Subject.Should().HaveCount(1);
    }

    // ── Test 6: Empty file list ───────────────────────────────────────────

    [Fact]
    public async Task EmptyFileList_Pipeline_ReturnsEmpty()
    {
        var pipeline = CreatePipeline();
        var previewService = CreatePreviewService(pipeline);
        var results = await previewService.PreviewAsync([], "{n}");
        results.Should().BeEmpty();
    }

    // ── Test 7: FileOrganization Test mode ────────────────────────────────

    [Fact]
    public async Task FileOrganization_TestMode_NoFileSystemCalls()
    {
        SetupMovieProvider("Inception", 2010, 27205);

        var pipeline = CreatePipeline();
        var previewService = CreatePreviewService(pipeline);
        var orgService = CreateOrganizationService(previewService);
        var results = await orgService.OrganizeAsync(
            ["Inception.2010.mkv"], "{n} ({y})", RenameAction.Test);
        results.Should().HaveCount(1);
        _fileSystem.Verify(
            f => f.MoveFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _fileSystem.Verify(
            f => f.CopyFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── Test 8: FileOrganization Move mode ────────────────────────────────

    [Fact]
    public async Task FileOrganization_MoveMode_CallsFileSystem()
    {
        SetupMovieProvider("Inception", 2010, 27205);

        var pipeline = CreatePipeline();
        var previewService = CreatePreviewService(pipeline);
        var orgService = CreateOrganizationService(previewService);

        _fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        var results = await orgService.OrganizeAsync(
            ["Inception.2010.mkv"], "{n} ({y})", RenameAction.Move);
        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        _fileSystem.Verify(
            f => f.MoveFile(It.IsAny<string>(), It.Is<string>(s => s.Contains("Inception (2010)"))),
            Times.Once);
    }
}
