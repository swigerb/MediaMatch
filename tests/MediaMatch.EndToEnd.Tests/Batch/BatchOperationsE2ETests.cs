using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using MediaMatch.EndToEnd.Tests.Fixtures;
using Moq;

namespace MediaMatch.EndToEnd.Tests.Batch;

/// <summary>
/// E2E: Batch operations — queue files → process → verify results → undo → verify restored.
/// </summary>
public class BatchOperationsE2ETests : IDisposable
{
    private readonly MediaMatchFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private BatchOperationService CreateBatchService(int maxConcurrency = 2)
    {
        _fixture.FileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        var orgService = _fixture.CreateOrganizationService();
        return new BatchOperationService(orgService, maxConcurrency: maxConcurrency);
    }

    // ── Queue and process ─────────────────────────────────────────────────

    [Fact]
    public async Task Batch_MultipleEpisodes_AllProcessed()
    {
        _fixture.SetupEpisodeProvider("Breaking Bad", 1, new List<Episode>
        {
            new("Breaking Bad", 1, 1, "Pilot"),
            new("Breaking Bad", 1, 2, "Cat's in the Bag..."),
            new("Breaking Bad", 1, 3, "...And the Bag's in the River"),
        });

        var batch = CreateBatchService();
        var files = new[]
        {
            "Breaking.Bad.S01E01.mkv",
            "Breaking.Bad.S01E02.mkv",
            "Breaking.Bad.S01E03.mkv",
        };

        var job = await batch.ExecuteAsync(files, "{n} - {s00e00} - {t}");

        job.Status.Should().Be(BatchStatus.Completed);
        job.CompletedCount.Should().Be(3);
        job.FailedCount.Should().Be(0);
        job.Files.Should().OnlyContain(f => f.Status == BatchFileStatus.Success);
    }

    [Fact]
    public async Task Batch_MixedMovieAndEpisode_ProcessesAll()
    {
        _fixture.SetupEpisodeProvider("Breaking Bad", 1, new List<Episode>
        {
            new("Breaking Bad", 1, 1, "Pilot"),
        });
        _fixture.SetupMovieProvider("Inception", 2010, 27205);

        var batch = CreateBatchService();
        var files = new[] { "Breaking.Bad.S01E01.mkv", "Inception.2010.mkv" };

        var job = await batch.ExecuteAsync(files, "{n}");

        job.Status.Should().Be(BatchStatus.Completed);
        job.Files.Should().HaveCount(2);
    }

    [Fact]
    public async Task Batch_EmptyFileList_ReturnsWithZeroFiles()
    {
        var orgServiceMock = new Mock<IFileOrganizationService>();
        var batch = new BatchOperationService(orgServiceMock.Object);

        var job = await batch.ExecuteAsync([], "{n}");

        job.CompletedCount.Should().Be(0);
        job.FailedCount.Should().Be(0);
        job.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task Batch_SingleFile_ProcessesSuccessfully()
    {
        _fixture.SetupMovieProvider("Inception", 2010, 27205);
        var batch = CreateBatchService();

        var job = await batch.ExecuteAsync(["Inception.2010.mkv"], "{n} ({y})");

        job.Status.Should().Be(BatchStatus.Completed);
        job.CompletedCount.Should().Be(1);
    }

    // ── Progress reporting ────────────────────────────────────────────────

    [Fact]
    public async Task Batch_ProgressReported_ForEachFile()
    {
        _fixture.SetupEpisodeProvider("Breaking Bad", 1, new List<Episode>
        {
            new("Breaking Bad", 1, 1, "Pilot"),
            new("Breaking Bad", 1, 2, "Cat's in the Bag..."),
        });

        var progressReports = new List<BatchProgress>();
        var progress = new Progress<BatchProgress>(p => progressReports.Add(p));

        var batch = CreateBatchService();
        var files = new[] { "Breaking.Bad.S01E01.mkv", "Breaking.Bad.S01E02.mkv" };

        await batch.ExecuteAsync(files, "{n}", progress);

        progressReports.Should().NotBeEmpty();
        progressReports.Last().TotalFiles.Should().Be(2);
    }

    // ── Cancellation ──────────────────────────────────────────────────────

    [Fact]
    public async Task Batch_Cancellation_StopsMidBatch()
    {
        using var cts = new CancellationTokenSource();

        var callCount = 0;
        var orgServiceMock = new Mock<IFileOrganizationService>();
        orgServiceMock
            .Setup(s => s.OrganizeAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns<IReadOnlyList<string>, string, CancellationToken>(async (paths, pattern, ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    cts.Cancel();
                    await Task.Delay(10, ct);  // yield so cancellation propagates
                }
                return new List<FileOrganizationResult>
                {
                    new(paths[0], paths[0] + ".renamed", 0.9f, MediaType.Movie, [], true)
                };
            });

        var batch = new BatchOperationService(orgServiceMock.Object, maxConcurrency: 1);
        var files = Enumerable.Range(1, 5).Select(i => $"file{i}.mkv").ToList();

        var job = await batch.ExecuteAsync(files, "{n}", ct: cts.Token);

        job.Status.Should().Be(BatchStatus.Cancelled);
        // Some files should be skipped
        job.Files.Should().Contain(f => f.Status == BatchFileStatus.Skipped);
    }

    [Fact]
    public async Task Batch_AllFilesFailProvider_FailedCountReflected()
    {
        _fixture.SetupEmptyProviders();
        var batch = CreateBatchService();

        var files = new[] { "unknown1.mkv", "unknown2.mkv" };
        var job = await batch.ExecuteAsync(files, "{n}");

        // Unknown files → no match → failed
        job.FailedCount.Should().BeGreaterThan(0);
    }

    // ── Job metadata ──────────────────────────────────────────────────────

    [Fact]
    public async Task Batch_JobMetadata_PopulatedCorrectly()
    {
        _fixture.SetupMovieProvider("Inception", 2010, 27205);
        var batch = CreateBatchService();

        var before = DateTimeOffset.UtcNow;
        var job = await batch.ExecuteAsync(["Inception.2010.mkv"], "{n}");
        var after = DateTimeOffset.UtcNow;

        job.Id.Should().NotBeNullOrEmpty();
        job.StartedAt.Should().BeOnOrAfter(before);
        job.CompletedAt.Should().HaveValue();
        job.CompletedAt!.Value.Should().BeOnOrBefore(after.AddSeconds(5));
    }

    // ── Undo operations ───────────────────────────────────────────────────

    [Fact]
    public async Task UndoService_RecordAndUndo_ReversesFileMove()
    {
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        using var tempDir = new TempDirectoryFixture();
        var journalPath = Path.Combine(tempDir.RootPath, "undo.json");

        var undoService = new UndoService(fileSystemMock.Object, journalPath: journalPath);

        var entries = new List<UndoEntry>
        {
            new("original/movie.mkv", "renamed/Inception (2010).mkv", DateTimeOffset.UtcNow, MediaType.Movie),
            new("original/show.mkv", "renamed/Breaking Bad - S01E01.mkv", DateTimeOffset.UtcNow, MediaType.TvSeries),
        };

        await undoService.RecordAsync(entries);

        var canUndo = await undoService.CanUndoAsync();
        canUndo.Should().BeTrue();

        var undoneCount = await undoService.UndoAsync(2);
        undoneCount.Should().Be(2);

        fileSystemMock.Verify(
            f => f.MoveFile(It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task UndoService_UndoMoreThanRecorded_UndoesWhatExists()
    {
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        using var tempDir = new TempDirectoryFixture();
        var journalPath = Path.Combine(tempDir.RootPath, "undo.json");

        var undoService = new UndoService(fileSystemMock.Object, journalPath: journalPath);
        await undoService.RecordAsync(
        [
            new UndoEntry("orig.mkv", "new.mkv", DateTimeOffset.UtcNow, MediaType.Movie)
        ]);

        var count = await undoService.UndoAsync(10); // Request more than available

        count.Should().Be(1);
    }

    [Fact]
    public async Task UndoService_EmptyJournal_CanUndoFalse()
    {
        var fileSystemMock = new Mock<IFileSystem>();

        using var tempDir = new TempDirectoryFixture();
        var journalPath = Path.Combine(tempDir.RootPath, "undo.json");

        var undoService = new UndoService(fileSystemMock.Object, journalPath: journalPath);

        var canUndo = await undoService.CanUndoAsync();
        canUndo.Should().BeFalse();
    }

    [Fact]
    public async Task UndoService_GetJournal_ReturnsInReverseChronological()
    {
        var fileSystemMock = new Mock<IFileSystem>();

        using var tempDir = new TempDirectoryFixture();
        var journalPath = Path.Combine(tempDir.RootPath, "undo.json");

        var undoService = new UndoService(fileSystemMock.Object, journalPath: journalPath);

        var t1 = DateTimeOffset.UtcNow.AddMinutes(-5);
        var t2 = DateTimeOffset.UtcNow;

        await undoService.RecordAsync(
        [
            new UndoEntry("first.mkv", "first_new.mkv", t1, MediaType.Movie),
            new UndoEntry("second.mkv", "second_new.mkv", t2, MediaType.Movie),
        ]);

        var journal = await undoService.GetJournalAsync();

        journal.Should().HaveCount(2);
        // GetJournal reverses the list, so most recent first
        journal[0].OriginalPath.Should().Be("second.mkv");
        journal[1].OriginalPath.Should().Be("first.mkv");
    }

    [Fact]
    public async Task UndoService_FileNotFound_SkipsEntry()
    {
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        using var tempDir = new TempDirectoryFixture();
        var journalPath = Path.Combine(tempDir.RootPath, "undo.json");

        var undoService = new UndoService(fileSystemMock.Object, journalPath: journalPath);
        await undoService.RecordAsync(
        [
            new UndoEntry("orig.mkv", "missing_file.mkv", DateTimeOffset.UtcNow, MediaType.Movie)
        ]);

        var count = await undoService.UndoAsync();

        // File doesn't exist so undo is skipped; count = 0
        count.Should().Be(0);
        fileSystemMock.Verify(f => f.MoveFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
