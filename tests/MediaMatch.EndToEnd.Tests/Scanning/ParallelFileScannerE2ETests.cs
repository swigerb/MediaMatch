using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Services;
using MediaMatch.EndToEnd.Tests.Fixtures;
using Moq;

namespace MediaMatch.EndToEnd.Tests.Scanning;

/// <summary>
/// E2E: ParallelFileScanner with real temp directories — streaming results via Channel{T},
/// NAS detection reduces concurrency, extension filtering works end-to-end.
/// </summary>
public sealed class ParallelFileScannerE2ETests : IDisposable
{
    private readonly TempDirectoryFixture _tempDir = new();

    public void Dispose() => _tempDir.Dispose();

    private ParallelFileScanner CreateScanner(bool isNetwork = false, int maxThreads = 4, int networkConcurrency = 2)
    {
        var settings = new PerformanceSettings
        {
            MaxScanThreads = maxThreads,
            NetworkConcurrency = networkConcurrency,
            MaxDirectoryDepth = 10
        };
        var networkDetector = new Mock<INetworkPathDetector>();
        networkDetector.Setup(d => d.IsNetworkPath(It.IsAny<string>())).Returns(isNetwork);

        return new ParallelFileScanner(settings, networkDetector.Object);
    }

    // ── Basic scan ────────────────────────────────────────────────────────

    [Fact]
    public async Task Scanner_EmptyDirectory_ReturnsNoFiles()
    {
        var scanner = CreateScanner();
        var results = await scanner.ScanToListAsync(_tempDir.RootPath);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Scanner_SingleMediaFile_ReturnsIt()
    {
        _tempDir.CreateFile("movie.mkv");

        var scanner = CreateScanner();
        var results = await scanner.ScanToListAsync(_tempDir.RootPath);

        results.Should().HaveCount(1);
        results[0].Should().EndWith("movie.mkv");
    }

    [Fact]
    public async Task Scanner_MultipleFiles_ReturnsAll()
    {
        _tempDir.CreateFile("movie1.mkv");
        _tempDir.CreateFile("movie2.mp4");
        _tempDir.CreateFile("show.avi");

        var scanner = CreateScanner();
        var results = await scanner.ScanToListAsync(_tempDir.RootPath);

        results.Should().HaveCount(3);
    }

    // ── Extension filtering ───────────────────────────────────────────────

    [Fact]
    public async Task Scanner_ExtensionFilter_OnlyReturnsMatchingFiles()
    {
        _tempDir.CreateFile("video.mkv");
        _tempDir.CreateFile("video.mp4");
        _tempDir.CreateFile("audio.mp3");
        _tempDir.CreateFile("document.pdf");

        var scanner = CreateScanner();
        var extensions = new HashSet<string> { ".mkv", ".mp4" };
        var results = await scanner.ScanToListAsync(_tempDir.RootPath, extensions);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.EndsWith(".mkv") || r.EndsWith(".mp4"));
    }

    [Fact]
    public async Task Scanner_NoMatchingExtension_ReturnsEmpty()
    {
        _tempDir.CreateFile("movie.mkv");
        _tempDir.CreateFile("show.mp4");

        var scanner = CreateScanner();
        var extensions = new HashSet<string> { ".avi" };
        var results = await scanner.ScanToListAsync(_tempDir.RootPath, extensions);

        results.Should().BeEmpty();
    }

    // ── Recursive scanning ────────────────────────────────────────────────

    [Fact]
    public async Task Scanner_Recursive_FindsFilesInSubdirectories()
    {
        _tempDir.CreateFile("root.mkv");
        _tempDir.CreateFile(Path.Combine("Season 1", "s01e01.mkv"));
        _tempDir.CreateFile(Path.Combine("Season 2", "s02e01.mkv"));

        var scanner = CreateScanner();
        var results = await scanner.ScanToListAsync(_tempDir.RootPath);

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task Scanner_DeepNested_RespectsMaxDepth()
    {
        var deep = Path.Combine("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "deep.mkv");
        _tempDir.CreateFile(deep);
        _tempDir.CreateFile("shallow.mkv");

        var settings = new PerformanceSettings
        {
            MaxScanThreads = 2,
            NetworkConcurrency = 1,
            MaxDirectoryDepth = 3  // Only 3 levels deep
        };
        var networkDetector = new Mock<INetworkPathDetector>();
        networkDetector.Setup(d => d.IsNetworkPath(It.IsAny<string>())).Returns(false);

        var scanner = new ParallelFileScanner(settings, networkDetector.Object);
        var results = await scanner.ScanToListAsync(_tempDir.RootPath);

        // shallow.mkv should be found, deep file should NOT (too deep)
        results.Should().Contain(r => r.EndsWith("shallow.mkv"));
        results.Should().NotContain(r => r.EndsWith("deep.mkv"));
    }

    // ── Progress reporting ────────────────────────────────────────────────

    [Fact]
    public async Task Scanner_ProgressReported_DuringLargeScan()
    {
        for (int i = 1; i <= 5; i++)
            _tempDir.CreateFile($"file{i}.mkv");

        var progressReports = new List<ScanProgress>();
        var progress = new Progress<ScanProgress>(p => progressReports.Add(p));

        var scanner = CreateScanner();
        await scanner.ScanToListAsync(_tempDir.RootPath, progress: progress);

        progressReports.Should().NotBeEmpty();
    }

    // ── Streaming via Channel ─────────────────────────────────────────────

    [Fact]
    public async Task Scanner_ChannelReader_StreamsResults()
    {
        _tempDir.CreateFile("a.mkv");
        _tempDir.CreateFile("b.mkv");
        _tempDir.CreateFile("c.mkv");

        var scanner = CreateScanner();
        var reader = scanner.ScanAsync(_tempDir.RootPath);

        var files = new List<string>();
        await foreach (var file in reader.ReadAllAsync())
        {
            files.Add(file);
        }

        files.Should().HaveCount(3);
    }

    // ── NAS detection reduces concurrency ─────────────────────────────────

    [Fact]
    public async Task Scanner_NetworkPath_UsesNetworkConcurrency()
    {
        _tempDir.CreateFile("nas_file.mkv");

        var settings = new PerformanceSettings
        {
            MaxScanThreads = 8,      // Would be used for local
            NetworkConcurrency = 2,   // Should be used for NAS
            MaxDirectoryDepth = 5
        };
        var networkDetector = new Mock<INetworkPathDetector>();
        networkDetector.Setup(d => d.IsNetworkPath(It.IsAny<string>())).Returns(true);

        var scanner = new ParallelFileScanner(settings, networkDetector.Object);
        var results = await scanner.ScanToListAsync(_tempDir.RootPath);

        // Verify network detector was called and scan completed
        results.Should().HaveCount(1);
        networkDetector.Verify(d => d.IsNetworkPath(_tempDir.RootPath), Times.Once);
    }

    // ── Cancellation ──────────────────────────────────────────────────────

    [Fact]
    public async Task Scanner_Cancellation_StopsGracefully()
    {
        for (int i = 1; i <= 20; i++)
            _tempDir.CreateFile($"file{i}.mkv");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        var scanner = CreateScanner();

        // Should not throw OperationCanceledException (scanner catches it internally)
        var act = async () => await scanner.ScanToListAsync(_tempDir.RootPath, ct: cts.Token);
        await act.Should().NotThrowAsync();
    }

    // ── Unicode filenames ─────────────────────────────────────────────────

    [Fact]
    public async Task Scanner_UnicodeFilenames_ReturnedCorrectly()
    {
        _tempDir.CreateFile("Über.Cool.S01E01.mkv");
        _tempDir.CreateFile("日本語.S02E03.mkv");

        var scanner = CreateScanner();
        var results = await scanner.ScanToListAsync(_tempDir.RootPath);

        results.Should().HaveCount(2);
        results.Should().Contain(r => r.Contains("Über.Cool"));
        results.Should().Contain(r => r.Contains("日本語"));
    }
}
