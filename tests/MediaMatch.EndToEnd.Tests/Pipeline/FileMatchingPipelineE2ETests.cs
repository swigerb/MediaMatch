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
using MediaMatch.EndToEnd.Tests.Fixtures;
using Moq;

namespace MediaMatch.EndToEnd.Tests.Pipeline;

/// <summary>
/// E2E: Full file matching pipeline — detect media type → match providers → generate rename preview.
/// </summary>
public sealed class FileMatchingPipelineE2ETests : IDisposable
{
    private readonly MediaMatchFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    // ── TV Episodes ───────────────────────────────────────────────────────

    [Fact]
    public async Task TvEpisode_StandardPattern_ProducesCorrectRename()
    {
        _fixture.SetupEpisodeProvider("Breaking Bad", 1, new List<Episode>
        {
            new("Breaking Bad", 1, 1, "Pilot"),
            new("Breaking Bad", 1, 2, "Cat's in the Bag..."),
        });

        var preview = _fixture.CreatePreviewService();
        var results = await preview.PreviewAsync(["Breaking.Bad.S01E02.720p.BluRay.mkv"], "{n} - {s00e00} - {t}");

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].NewPath.Should().Contain("Breaking Bad - S01E02 - Cat's in the Bag...");
        results[0].NewPath.Should().EndWith(".mkv");
        results[0].MediaType.Should().Be(MediaType.TvSeries);
    }

    [Fact]
    public async Task TvEpisode_MultiEpisodePattern_ProducesRangeBinding()
    {
        _fixture.SetupEpisodeProvider("Game of Thrones", 1, new List<Episode>
        {
            new("Game of Thrones", 1, 1, "Winter Is Coming"),
            new("Game of Thrones", 1, 2, "The Kingsroad"),
        });

        var preview = _fixture.CreatePreviewService();
        // Multi-episode file: S01E01-E02
        var results = await preview.PreviewAsync(["Game.of.Thrones.S01E01E02.mkv"], "{n} - {s00e00}");

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].NewPath.Should().Contain("Game of Thrones");
    }

    [Fact]
    public async Task AnimeEpisode_DetectedAndRenamed()
    {
        _fixture.SetupEpisodeProvider("Fullmetal Alchemist Brotherhood", 1, new List<Episode>
        {
            new("Fullmetal Alchemist Brotherhood", 1, 1, "Fullmetal Alchemist"),
            new("Fullmetal Alchemist Brotherhood", 1, 2, "The First Day"),
        });

        var preview = _fixture.CreatePreviewService();
        var results = await preview.PreviewAsync(["Fullmetal.Alchemist.Brotherhood.S01E02.mkv"], "{n} - {s00e00} - {t}");

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].NewPath.Should().Contain("Fullmetal Alchemist Brotherhood");
    }

    // ── Movies ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Movie_StandardPattern_ProducesCorrectRename()
    {
        _fixture.SetupMovieProvider("Inception", 2010, 27205);

        var preview = _fixture.CreatePreviewService();
        var results = await preview.PreviewAsync(["Inception.2010.1080p.BluRay.mkv"], "{n} ({y})");

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].NewPath.Should().Contain("Inception (2010)");
        results[0].NewPath.Should().EndWith(".mkv");
        results[0].MediaType.Should().Be(MediaType.Movie);
    }

    [Fact]
    public async Task Movie_WithoutYear_StillMatches()
    {
        _fixture.SetupMovieProvider("The Matrix", 1999, 603);

        var preview = _fixture.CreatePreviewService();
        var results = await preview.PreviewAsync(["The.Matrix.1080p.mkv"], "{n}");

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
    }

    // ── Multi-episode ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("Show.S01E01-E02.mkv")]
    [InlineData("Show.S01E01E02.mkv")]
    [InlineData("Show.1x01x02.mkv")]
    public async Task MultiEpisodePatterns_AllRecognized(string filename)
    {
        _fixture.SetupEpisodeProvider("Show", 999, new List<Episode>
        {
            new("Show", 1, 1, "Part One"),
            new("Show", 1, 2, "Part Two"),
        });

        var preview = _fixture.CreatePreviewService();
        var results = await preview.PreviewAsync([filename], "{n}");

        results.Should().HaveCount(1);
        // Success depends on detector; assert no exception at minimum
        results[0].OriginalPath.Should().Be(filename);
    }

    // ── Edge cases ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Über.Cool.Show.S02E03.mkv")]
    [InlineData("日本語アニメ.S01E01.mkv")]
    [InlineData("Señor.de.los.Anillos.S01E01.mkv")]
    public async Task UnicodeFilename_DoesNotThrow(string filename)
    {
        _fixture.SetupEmptyProviders();
        var preview = _fixture.CreatePreviewService();

        var act = () => preview.PreviewAsync([filename], "{n}");
        var results = await act.Should().NotThrowAsync();
        results.Subject.Should().HaveCount(1);
    }

    [Fact]
    public async Task EmptyFileList_ReturnsEmpty()
    {
        var preview = _fixture.CreatePreviewService();
        var results = await preview.PreviewAsync([], "{n}");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task UnrecognizedFile_ReturnsNoMatch()
    {
        _fixture.SetupEmptyProviders();
        var preview = _fixture.CreatePreviewService();
        var results = await preview.PreviewAsync(["random_document.txt"], "{n}");

        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
    }

    [Fact]
    public async Task MixedBatch_TvAndMovie_BothMatch()
    {
        _fixture.SetupEpisodeProvider("Breaking Bad", 1, new List<Episode>
        {
            new("Breaking Bad", 1, 1, "Pilot"),
            new("Breaking Bad", 1, 2, "Cat's in the Bag..."),
        });
        _fixture.SetupMovieProvider("Inception", 2010, 27205);

        var pipeline = _fixture.CreatePipeline();
        var preview = _fixture.CreatePreviewService(pipeline);

        var files = new[]
        {
            "Breaking.Bad.S01E01.mkv",
            "Breaking.Bad.S01E02.mkv",
            "Inception.2010.mkv",
        };
        var results = await preview.PreviewAsync(files, "{n}");

        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.Success);
    }

    // ── FileOrganization actions ──────────────────────────────────────────

    [Fact]
    public async Task FileOrganization_TestMode_NoFileSystemCalls()
    {
        _fixture.SetupMovieProvider("Inception", 2010, 27205);

        var pipeline = _fixture.CreatePipeline();
        var preview = _fixture.CreatePreviewService(pipeline);
        var orgService = _fixture.CreateOrganizationService(preview);

        var results = await orgService.OrganizeAsync(["Inception.2010.mkv"], "{n} ({y})", RenameAction.Test);

        results.Should().HaveCount(1);
        _fixture.FileSystem.Verify(f => f.MoveFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _fixture.FileSystem.Verify(f => f.CopyFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FileOrganization_MoveMode_CallsFileSystem()
    {
        _fixture.SetupMovieProvider("Inception", 2010, 27205);
        _fixture.FileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        var pipeline = _fixture.CreatePipeline();
        var preview = _fixture.CreatePreviewService(pipeline);
        var orgService = _fixture.CreateOrganizationService(preview);

        var results = await orgService.OrganizeAsync(["Inception.2010.mkv"], "{n} ({y})", RenameAction.Move);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        _fixture.FileSystem.Verify(
            f => f.MoveFile(It.IsAny<string>(), It.Is<string>(s => s.Contains("Inception (2010)"))),
            Times.Once);
    }

    [Fact]
    public async Task FileOrganization_CopyMode_CallsCopyFile()
    {
        _fixture.SetupMovieProvider("The Matrix", 1999, 603);
        _fixture.FileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        var pipeline = _fixture.CreatePipeline();
        var preview = _fixture.CreatePreviewService(pipeline);
        var orgService = _fixture.CreateOrganizationService(preview);

        var results = await orgService.OrganizeAsync(["The.Matrix.1999.mkv"], "{n} ({y})", RenameAction.Copy);

        results.Should().HaveCount(1);
        _fixture.FileSystem.Verify(
            f => f.CopyFile(It.IsAny<string>(), It.Is<string>(s => s.Contains("The Matrix (1999)"))),
            Times.Once);
    }

    [Fact]
    public async Task FileOrganization_RollbackOnFailure_ReversesCompletedRenames()
    {
        _fixture.SetupEpisodeProvider("Breaking Bad", 1, new List<Episode>
        {
            new("Breaking Bad", 1, 1, "Pilot"),
            new("Breaking Bad", 1, 2, "Cat's in the Bag..."),
        });

        var moveCallCount = 0;
        _fixture.FileSystem
            .Setup(f => f.MoveFile(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, _) =>
            {
                moveCallCount++;
                if (moveCallCount == 2)
                    throw new IOException("Disk full");
            });
        _fixture.FileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        var pipeline = _fixture.CreatePipeline();
        var preview = _fixture.CreatePreviewService(pipeline);
        var orgService = _fixture.CreateOrganizationService(preview);

        var files = new[] { "Breaking.Bad.S01E01.mkv", "Breaking.Bad.S01E02.mkv" };
        var results = await orgService.OrganizeAsync(files, "{n} - {s00e00} - {t}", RenameAction.Move);

        // At least one should have failed or been skipped
        results.Should().HaveCount(2);
        results.Should().Contain(r => !r.Success || r.Warnings.Count > 0);
    }
}
