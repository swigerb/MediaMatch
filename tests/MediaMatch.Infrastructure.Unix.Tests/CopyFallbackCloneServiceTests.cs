using FluentAssertions;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Services;
using MediaMatch.Infrastructure.Unix.FileSystem;
using Moq;

namespace MediaMatch.Infrastructure.Unix.Tests;

public sealed class CopyFallbackCloneServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<IUnixHardLinkHandler> _mockHardLinkHandler;
    private readonly CopyFallbackCloneService _service;

    public CopyFallbackCloneServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MediaMatch_CloneTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _mockHardLinkHandler = new Mock<IUnixHardLinkHandler>();
        _service = new CopyFallbackCloneService(_mockHardLinkHandler.Object);
    }

    [Fact]
    public void CloneFile_WhenHardLinkFails_FallsBackToCopy()
    {
        // Arrange
        var source = Path.Combine(_tempDir, "source.txt");
        var dest = Path.Combine(_tempDir, "dest.txt");
        File.WriteAllText(source, "test content");
        _mockHardLinkHandler.Setup(h => h.TryCreateHardLinkWithResult(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(HardLinkResult.FilesystemUnsupported);

        // Act
        var result = _service.CloneFile(source, dest);

        // Assert
        result.Should().Be(CloneCapability.Copy);
        File.Exists(dest).Should().BeTrue();
        File.ReadAllText(dest).Should().Be("test content");
    }

    [Fact]
    public void CloneFile_WhenHardLinkSucceeds_ReturnsHardLink()
    {
        // Arrange
        var source = Path.Combine(_tempDir, "source2.txt");
        var dest = Path.Combine(_tempDir, "dest2.txt");
        File.WriteAllText(source, "test content");
        _mockHardLinkHandler.Setup(h => h.TryCreateHardLinkWithResult(dest, source))
            .Returns(HardLinkResult.Success);

        // Act
        var result = _service.CloneFile(source, dest);

        // Assert
        result.Should().Be(CloneCapability.HardLink);
    }

    [Fact]
    public void CloneFile_CreatesDestinationDirectory()
    {
        // Arrange
        var source = Path.Combine(_tempDir, "source3.txt");
        var dest = Path.Combine(_tempDir, "subdir", "nested", "dest3.txt");
        File.WriteAllText(source, "test content");
        _mockHardLinkHandler.Setup(h => h.TryCreateHardLinkWithResult(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(HardLinkResult.FilesystemUnsupported);

        // Act
        _service.CloneFile(source, dest);

        // Assert
        File.Exists(dest).Should().BeTrue();
        File.ReadAllText(dest).Should().Be("test content");
    }

    [Fact]
    public void CloneFile_CopyOverwritesExisting()
    {
        // Arrange
        var source = Path.Combine(_tempDir, "source4.txt");
        var dest = Path.Combine(_tempDir, "dest4.txt");
        File.WriteAllText(source, "new content");
        File.WriteAllText(dest, "old content");
        _mockHardLinkHandler.Setup(h => h.TryCreateHardLinkWithResult(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(HardLinkResult.FilesystemUnsupported);

        // Act
        _service.CloneFile(source, dest);

        // Assert
        File.ReadAllText(dest).Should().Be("new content");
    }

    [Fact]
    public void CloneFile_PathSpecificFailure_DoesNotPoisonCache()
    {
        // Arrange — first call fails for path-specific reason, second succeeds.
        var source1 = Path.Combine(_tempDir, "source-a.txt");
        var dest1 = Path.Combine(_tempDir, "dest-a.txt");
        var source2 = Path.Combine(_tempDir, "source-b.txt");
        var dest2 = Path.Combine(_tempDir, "dest-b.txt");
        File.WriteAllText(source1, "a");
        File.WriteAllText(source2, "b");

        _mockHardLinkHandler.Setup(h => h.TryCreateHardLinkWithResult(dest1, source1))
            .Returns(HardLinkResult.PathSpecificFailure);
        _mockHardLinkHandler.Setup(h => h.TryCreateHardLinkWithResult(dest2, source2))
            .Returns(HardLinkResult.Success);

        // Act
        var r1 = _service.CloneFile(source1, dest1);
        var r2 = _service.CloneFile(source2, dest2);

        // Assert — second attempt must still try the hard link (cache not poisoned).
        r1.Should().Be(CloneCapability.Copy);
        r2.Should().Be(CloneCapability.HardLink);
        _mockHardLinkHandler.Verify(
            h => h.TryCreateHardLinkWithResult(dest2, source2), Times.Once);
    }

    [Fact]
    public void CloneFile_FilesystemUnsupported_PoisonsCacheForMount()
    {
        // Arrange — first call reports filesystem doesn't support hard links.
        var source1 = Path.Combine(_tempDir, "fs-a.txt");
        var dest1 = Path.Combine(_tempDir, "fs-dest-a.txt");
        var source2 = Path.Combine(_tempDir, "fs-b.txt");
        var dest2 = Path.Combine(_tempDir, "fs-dest-b.txt");
        File.WriteAllText(source1, "a");
        File.WriteAllText(source2, "b");

        _mockHardLinkHandler.Setup(h => h.TryCreateHardLinkWithResult(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(HardLinkResult.FilesystemUnsupported);

        // Act
        _service.CloneFile(source1, dest1);
        _service.CloneFile(source2, dest2);

        // Assert — second clone should skip hard-link attempt entirely.
        _mockHardLinkHandler.Verify(
            h => h.TryCreateHardLinkWithResult(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort cleanup */ }
    }
}
