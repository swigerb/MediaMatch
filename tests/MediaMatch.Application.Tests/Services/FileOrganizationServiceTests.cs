using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Moq;

namespace MediaMatch.Application.Tests.Services;

public sealed class FileOrganizationServiceTests
{
    private readonly Mock<IRenamePreviewService> _previewService = new();
    private readonly Mock<IFileSystem> _fileSystem = new();

    private FileOrganizationService CreateService() =>
        new(_previewService.Object, _fileSystem.Object);

    [Fact]
    public async Task OrganizeAsync_SuccessfulRename_MovesFiles()
    {
        var previews = new List<FileOrganizationResult>
        {
            new("original.mkv", "New Name.mkv", 0.9f, MediaType.Movie, [], true)
        };

        _previewService
            .Setup(p => p.PreviewAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previews);
        _fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        var sut = CreateService();
        var results = await sut.OrganizeAsync(["original.mkv"], "{n}");

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        _fileSystem.Verify(f => f.MoveFile("original.mkv", "New Name.mkv"), Times.Once);
    }

    [Fact]
    public async Task OrganizeAsync_TestAction_DoesNotMoveFiles()
    {
        var previews = new List<FileOrganizationResult>
        {
            new("original.mkv", "New Name.mkv", 0.9f, MediaType.Movie, [], true)
        };

        _previewService
            .Setup(p => p.PreviewAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previews);

        var sut = CreateService();
        var results = await sut.OrganizeAsync(["original.mkv"], "{n}", RenameAction.Test);

        results.Should().HaveCount(1);
        _fileSystem.Verify(f => f.MoveFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OrganizeAsync_CopyAction_CopiesFiles()
    {
        var previews = new List<FileOrganizationResult>
        {
            new("original.mkv", "New Name.mkv", 0.9f, MediaType.Movie, [], true)
        };

        _previewService
            .Setup(p => p.PreviewAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previews);

        var sut = CreateService();
        var results = await sut.OrganizeAsync(["original.mkv"], "{n}", RenameAction.Copy);

        _fileSystem.Verify(f => f.CopyFile("original.mkv", "New Name.mkv"), Times.Once);
    }

    [Fact]
    public async Task OrganizeAsync_RenameFailure_RollsBack()
    {
        var previews = new List<FileOrganizationResult>
        {
            new("file1.mkv", "new1.mkv", 0.9f, MediaType.Movie, [], true),
            new("file2.mkv", "new2.mkv", 0.9f, MediaType.Movie, [], true),
        };

        _previewService
            .Setup(p => p.PreviewAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previews);

        // First move succeeds, second throws
        var moveCallCount = 0;
        _fileSystem
            .Setup(f => f.MoveFile(It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() =>
            {
                moveCallCount++;
                if (moveCallCount == 2)
                    throw new IOException("Disk full");
            });
        _fileSystem.Setup(f => f.FileExists("new1.mkv")).Returns(true);

        var sut = CreateService();
        var results = await sut.OrganizeAsync(["file1.mkv", "file2.mkv"], "{n}");

        // Should have rolled back the first successful move
        _fileSystem.Verify(f => f.MoveFile("new1.mkv", "file1.mkv"), Times.Once);
        results.Should().Contain(r => r.Warnings.Any(w => w.Contains("Rename failed")));
    }

    [Fact]
    public async Task OrganizeAsync_EmptyFileList_ReturnsEmpty()
    {
        var sut = CreateService();
        var results = await sut.OrganizeAsync([], "{n}");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task OrganizeAsync_SameSourceAndDest_Skips()
    {
        var previews = new List<FileOrganizationResult>
        {
            new("same.mkv", "same.mkv", 0.9f, MediaType.Movie, [], true)
        };

        _previewService
            .Setup(p => p.PreviewAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previews);

        var sut = CreateService();
        var results = await sut.OrganizeAsync(["same.mkv"], "{n}");

        _fileSystem.Verify(f => f.MoveFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        results[0].Warnings.Should().Contain(w => w.Contains("identical"));
    }

    [Fact]
    public async Task OrganizeAsync_FailedPreview_PassesThroughWithoutRename()
    {
        var previews = new List<FileOrganizationResult>
        {
            FileOrganizationResult.Failed("bad.file", "No match found")
        };

        _previewService
            .Setup(p => p.PreviewAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previews);

        var sut = CreateService();
        var results = await sut.OrganizeAsync(["bad.file"], "{n}");

        results[0].Success.Should().BeFalse();
        _fileSystem.Verify(f => f.MoveFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OrganizeAsync_CreatesDestinationDirectory()
    {
        var previews = new List<FileOrganizationResult>
        {
            new("original.mkv", Path.Combine("Movies", "New Name.mkv"), 0.9f, MediaType.Movie, [], true)
        };

        _previewService
            .Setup(p => p.PreviewAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previews);

        var sut = CreateService();
        await sut.OrganizeAsync(["original.mkv"], "{n}");

        _fileSystem.Verify(f => f.CreateDirectory("Movies"), Times.Once);
    }

    [Fact]
    public async Task OrganizeAsync_Cancellation_RollsBackAndThrows()
    {
        var cts = new CancellationTokenSource();

        var previews = new List<FileOrganizationResult>
        {
            new("file1.mkv", "new1.mkv", 0.9f, MediaType.Movie, [], true),
            new("file2.mkv", "new2.mkv", 0.9f, MediaType.Movie, [], true),
        };

        _previewService
            .Setup(p => p.PreviewAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previews);

        // Cancel after first move
        _fileSystem
            .Setup(f => f.MoveFile("file1.mkv", "new1.mkv"))
            .Callback(() => cts.Cancel());
        _fileSystem.Setup(f => f.FileExists("new1.mkv")).Returns(true);

        var sut = CreateService();
        var act = () => sut.OrganizeAsync(["file1.mkv", "file2.mkv"], "{n}", RenameAction.Move, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _fileSystem.Verify(f => f.MoveFile("new1.mkv", "file1.mkv"), Times.Once);
    }

    [Fact]
    public async Task OrganizeAsync_NullPattern_ThrowsArgumentException()
    {
        var sut = CreateService();
        var act = () => sut.OrganizeAsync(["file.mkv"], null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task OrganizeAsync_HardlinkAction_CreatesHardlink()
    {
        var previews = new List<FileOrganizationResult>
        {
            new("original.mkv", "link.mkv", 0.9f, MediaType.Movie, [], true)
        };

        _previewService
            .Setup(p => p.PreviewAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previews);

        var sut = CreateService();
        await sut.OrganizeAsync(["original.mkv"], "{n}", RenameAction.Hardlink);

        _fileSystem.Verify(f => f.CreateHardLink("link.mkv", "original.mkv"), Times.Once);
    }
}
