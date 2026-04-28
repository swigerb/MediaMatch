using FluentAssertions;
using MediaMatch.Application.Expressions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Expressions;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Moq;

namespace MediaMatch.Application.Tests.Services;

public sealed class RenamePreviewServiceTests
{
    private readonly Mock<IMatchingPipeline> _pipeline = new();
    private readonly IExpressionEngine _expressionEngine = new ScribanExpressionEngine();

    private RenamePreviewService CreateService() =>
        new(_pipeline.Object, _expressionEngine);

    [Fact]
    public async Task PreviewAsync_EpisodeMatch_GeneratesCorrectPath()
    {
        var matchResult = new MatchResult(
            MediaType.TvSeries, 0.9f, "TestProvider",
            Episode: new Episode("Breaking Bad", 1, 2, "Cat's in the Bag..."),
            SeriesInfo: new SeriesInfo(
                "Breaking Bad", "1", null, null, null, null, null, [],
                StartDate: new SimpleDate(2008, 1, 20)));

        _pipeline
            .Setup(p => p.ProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchResult);

        var sut = CreateService();
        var results = await sut.PreviewAsync(
            ["Breaking.Bad.S01E02.mkv"],
            "{n} - {s00e00} - {t}");

        results.Should().HaveCount(1);
        var result = results[0];
        result.Success.Should().BeTrue();
        result.NewPath.Should().Contain("Breaking Bad - S01E02 - Cat's in the Bag...");
        result.NewPath.Should().EndWith(".mkv");
        result.MatchConfidence.Should().Be(0.9f);
    }

    [Fact]
    public async Task PreviewAsync_MovieMatch_GeneratesCorrectPath()
    {
        var matchResult = new MatchResult(
            MediaType.Movie, 0.85f, "TestProvider",
            Movie: new Movie("Inception", 2010, TmdbId: 27205));

        _pipeline
            .Setup(p => p.ProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchResult);

        var sut = CreateService();
        var results = await sut.PreviewAsync(
            ["Inception.2010.mkv"],
            "{n} ({y})");

        results.Should().HaveCount(1);
        var result = results[0];
        result.Success.Should().BeTrue();
        result.NewPath.Should().Contain("Inception (2010)");
    }

    [Fact]
    public async Task PreviewAsync_NoMatch_ReturnsFailedResult()
    {
        _pipeline
            .Setup(p => p.ProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MatchResult.NoMatch(MediaType.Unknown));

        var sut = CreateService();
        var results = await sut.PreviewAsync(["unknown.file"], "{n}");

        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        results[0].Warnings.Should().Contain(w => w.Contains("No match"));
    }

    [Fact]
    public async Task PreviewAsync_InvalidPattern_ReturnsFailedResults()
    {
        var sut = CreateService();
        var results = await sut.PreviewAsync(
            ["file.mkv"],
            "{{invalid unclosed");

        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        results[0].Warnings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PreviewAsync_EmptyFileList_ReturnsEmptyResults()
    {
        var sut = CreateService();
        var results = await sut.PreviewAsync([], "{n}");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewAsync_LowConfidence_IncludesWarning()
    {
        var matchResult = new MatchResult(
            MediaType.Movie, 0.3f, "TestProvider",
            Movie: new Movie("Maybe", 2020));

        _pipeline
            .Setup(p => p.ProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchResult);

        var sut = CreateService();
        var results = await sut.PreviewAsync(["maybe.mkv"], "{n} ({y})");

        results[0].Warnings.Should().Contain(w => w.Contains("Low confidence"));
    }

    [Fact]
    public async Task PreviewAsync_MultipleFiles_ReturnsResultsForEach()
    {
        var episodeMatch = new MatchResult(
            MediaType.TvSeries, 0.9f, "Provider",
            Episode: new Episode("Show", 1, 1, "Pilot"));
        var movieMatch = new MatchResult(
            MediaType.Movie, 0.85f, "Provider",
            Movie: new Movie("Film", 2020));

        var callCount = 0;
        _pipeline
            .Setup(p => p.ProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ == 0 ? episodeMatch : movieMatch);

        var sut = CreateService();
        var results = await sut.PreviewAsync(
            ["Show.S01E01.mkv", "Film.2020.mkv"],
            "{n}");

        results.Should().HaveCount(2);
        results[0].Success.Should().BeTrue();
        results[1].Success.Should().BeTrue();
    }

    [Fact]
    public async Task PreviewAsync_Cancellation_Throws()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = CreateService();
        var act = () => sut.PreviewAsync(["file.mkv"], "{n}", cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PreviewAsync_PreservesFileExtension()
    {
        var matchResult = new MatchResult(
            MediaType.Movie, 0.9f, "Provider",
            Movie: new Movie("Test", 2020));

        _pipeline
            .Setup(p => p.ProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchResult);

        var sut = CreateService();
        var results = await sut.PreviewAsync(["test.mp4"], "{n}");

        results[0].NewPath.Should().EndWith(".mp4");
    }

    [Fact]
    public async Task PreviewAsync_NullPattern_ThrowsArgumentException()
    {
        var sut = CreateService();
        var act = () => sut.PreviewAsync(["file.mkv"], null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task PreviewAsync_ScribanHelpers_WorkInPattern()
    {
        var matchResult = new MatchResult(
            MediaType.TvSeries, 0.9f, "Provider",
            Episode: new Episode("breaking bad", 1, 2, "Test"));

        _pipeline
            .Setup(p => p.ProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchResult);

        var sut = CreateService();
        var results = await sut.PreviewAsync(
            ["show.S01E02.mkv"],
            "{{mm.upper_first n}} - {s00e00}");

        results[0].Success.Should().BeTrue();
        results[0].NewPath.Should().Contain("Breaking bad - S01E02");
    }
}
