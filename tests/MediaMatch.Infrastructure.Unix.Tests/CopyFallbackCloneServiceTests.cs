using FluentAssertions;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Services;
using MediaMatch.Infrastructure.Unix.FileSystem;
using Moq;

namespace MediaMatch.Infrastructure.Unix.Tests;

public sealed class CopyFallbackCloneServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<IHardLinkHandler> _mockHardLinkHandler;
    private readonly CopyFallbackCloneService _service;

    public CopyFallbackCloneServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MediaMatch_CloneTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _mockHardLinkHandler = new Mock<IHardLinkHandler>();
        _service = new CopyFallbackCloneService(_mockHardLinkHandler.Object);
    }

    [Fact]
    public void CloneFile_WhenHardLinkFails_FallsBackToCopy()
    {
        // Arrange
        var source = Path.Combine(_tempDir, "source.txt");
        var dest = Path.Combine(_tempDir, "dest.txt");
        File.WriteAllText(source, "test content");
        _mockHardLinkHandler.Setup(h => h.TryCreateHardLink(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

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
        _mockHardLinkHandler.Setup(h => h.TryCreateHardLink(dest, source)).Returns(true);

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
        _mockHardLinkHandler.Setup(h => h.TryCreateHardLink(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

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
        _mockHardLinkHandler.Setup(h => h.TryCreateHardLink(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        // Act
        _service.CloneFile(source, dest);

        // Assert
        File.ReadAllText(dest).Should().Be("new content");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort cleanup */ }
    }
}
