using FluentAssertions;
using MediaMatch.App.ViewModels;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Moq;

namespace MediaMatch.App.Tests.ViewModels;

public sealed class MediaInfoInspectorViewModelTests
{
    private readonly Mock<IMediaInfoService> _mediaInfoServiceMock = new();

    private static MediaInfoResult CreateSampleResult() => new()
    {
        FilePath = @"C:\video\sample.mkv",
        General = new Dictionary<string, string>
        {
            ["Format"] = "Matroska",
            ["Duration"] = "1:30:00",
            ["FileSize"] = "4.2 GB"
        },
        VideoStreams =
        [
            new Dictionary<string, string>
            {
                ["Codec"] = "HEVC",
                ["Resolution"] = "3840x2160",
                ["BitDepth"] = "10"
            }
        ],
        AudioStreams =
        [
            new Dictionary<string, string>
            {
                ["Codec"] = "DTS-HD MA",
                ["Channels"] = "7.1",
                ["Language"] = "English"
            }
        ],
        TextStreams =
        [
            new Dictionary<string, string>
            {
                ["Codec"] = "SubRip",
                ["Language"] = "English"
            }
        ]
    };

    [Fact]
    public async Task LoadFile_PopulatesAllProperties()
    {
        // Arrange
        var result = CreateSampleResult();
        _mediaInfoServiceMock.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mediaInfoServiceMock.Setup(s => s.GetMediaInfoAsync(@"C:\video\sample.mkv", It.IsAny<CancellationToken>())).ReturnsAsync(result);

        var vm = new MediaInfoInspectorViewModel(_mediaInfoServiceMock.Object, null);

        // Act
        await vm.LoadFileCommand.ExecuteAsync(@"C:\video\sample.mkv");

        // Assert
        vm.FilePath.Should().Be(@"C:\video\sample.mkv");
        vm.IsLoading.Should().BeFalse();
        vm.ErrorMessage.Should().BeEmpty();
        vm.HasResult.Should().BeTrue();

        vm.GeneralProperties.Should().HaveCount(3);
        vm.GeneralProperties[0].Key.Should().Be("Format");
        vm.GeneralProperties[0].Value.Should().Be("Matroska");

        vm.VideoStreams.Should().HaveCount(1);
        vm.VideoStreams[0].Name.Should().Be("Video #1");
        vm.VideoStreams[0].Properties.Should().HaveCount(3);

        vm.AudioStreams.Should().HaveCount(1);
        vm.AudioStreams[0].Name.Should().Be("Audio #1");
        vm.AudioStreams[0].Properties.Should().HaveCount(3);

        vm.TextStreams.Should().HaveCount(1);
        vm.TextStreams[0].Name.Should().Be("Text #1");
        vm.TextStreams[0].Properties.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadFile_FfprobeUnavailable_SetsErrorMessage()
    {
        // Arrange
        _mediaInfoServiceMock.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var vm = new MediaInfoInspectorViewModel(_mediaInfoServiceMock.Object, null);

        // Act
        await vm.LoadFileCommand.ExecuteAsync(@"C:\video\sample.mkv");

        // Assert
        vm.IsFfprobeAvailable.Should().BeFalse();
        vm.ErrorMessage.Should().Contain("ffprobe");
        vm.ErrorMessage.Should().Contain("ffmpeg.org");
        vm.HasResult.Should().BeFalse();
        vm.GeneralProperties.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadFile_NullResult_ShowsError()
    {
        // Arrange
        _mediaInfoServiceMock.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mediaInfoServiceMock.Setup(s => s.GetMediaInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((MediaInfoResult?)null);

        var vm = new MediaInfoInspectorViewModel(_mediaInfoServiceMock.Object, null);

        // Act
        await vm.LoadFileCommand.ExecuteAsync(@"C:\video\notamedia.txt");

        // Assert
        vm.ErrorMessage.Should().Contain("Could not read");
        vm.HasResult.Should().BeFalse();
    }

    [Fact]
    public async Task LoadFile_ServiceThrows_ShowsError()
    {
        // Arrange
        _mediaInfoServiceMock.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mediaInfoServiceMock.Setup(s => s.GetMediaInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var vm = new MediaInfoInspectorViewModel(_mediaInfoServiceMock.Object, null);

        // Act
        await vm.LoadFileCommand.ExecuteAsync(@"C:\video\broken.mkv");

        // Assert
        vm.ErrorMessage.Should().Contain("boom");
        vm.IsLoading.Should().BeFalse();
        vm.HasResult.Should().BeFalse();
    }

    [Fact]
    public async Task CopyToClipboard_WithResult_CallsExportAsText()
    {
        // Arrange
        var result = CreateSampleResult();
        _mediaInfoServiceMock.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mediaInfoServiceMock.Setup(s => s.GetMediaInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);

        string? copiedText = null;
        var vm = new TestableMediaInfoInspectorViewModel(_mediaInfoServiceMock.Object, t => copiedText = t);
        await vm.LoadFileCommand.ExecuteAsync(@"C:\video\sample.mkv");

        // Act
        vm.CopyToClipboardCommand.Execute(null);

        // Assert
        copiedText.Should().NotBeNullOrEmpty();
        copiedText.Should().Contain("File: C:\\video\\sample.mkv");
        copiedText.Should().Contain("Format: Matroska");
        copiedText.Should().Contain("Codec: HEVC");
    }

    [Fact]
    public void CopyToClipboard_WithoutResult_DoesNothing()
    {
        // Arrange
        string? copiedText = null;
        var vm = new TestableMediaInfoInspectorViewModel(_mediaInfoServiceMock.Object, t => copiedText = t);

        // Act
        vm.CopyToClipboardCommand.Execute(null);

        // Assert
        copiedText.Should().BeNull();
    }

    [Fact]
    public async Task LoadFile_EmptyPath_DoesNothing()
    {
        // Arrange
        var vm = new MediaInfoInspectorViewModel(_mediaInfoServiceMock.Object, null);

        // Act
        await vm.LoadFileCommand.ExecuteAsync("");

        // Assert
        vm.FilePath.Should().BeEmpty();
        _mediaInfoServiceMock.Verify(s => s.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadFile_SecondLoad_ClearsPreviousData()
    {
        // Arrange
        var result1 = CreateSampleResult();
        var result2 = new MediaInfoResult
        {
            FilePath = @"C:\video\other.mp4",
            General = new Dictionary<string, string> { ["Format"] = "MP4" },
            VideoStreams = [],
            AudioStreams = [],
            TextStreams = []
        };

        _mediaInfoServiceMock.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mediaInfoServiceMock.SetupSequence(s => s.GetMediaInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result1)
            .ReturnsAsync(result2);

        var vm = new MediaInfoInspectorViewModel(_mediaInfoServiceMock.Object, null);

        // Act
        await vm.LoadFileCommand.ExecuteAsync(@"C:\video\sample.mkv");
        vm.VideoStreams.Should().HaveCount(1);

        await vm.LoadFileCommand.ExecuteAsync(@"C:\video\other.mp4");

        // Assert — previous video streams cleared
        vm.GeneralProperties.Should().HaveCount(1);
        vm.GeneralProperties[0].Value.Should().Be("MP4");
        vm.VideoStreams.Should().BeEmpty();
        vm.AudioStreams.Should().BeEmpty();
        vm.TextStreams.Should().BeEmpty();
    }

    /// <summary>
    /// Testable subclass that overrides clipboard access to avoid WinUI thread requirement.
    /// </summary>
    private sealed class TestableMediaInfoInspectorViewModel : MediaInfoInspectorViewModel
    {
        private readonly Action<string> _onCopy;

        public TestableMediaInfoInspectorViewModel(IMediaInfoService service, Action<string> onCopy)
            : base(service, null)
        {
            _onCopy = onCopy;
        }

        internal override void CopyTextToClipboard(string text) => _onCopy(text);
    }
}
