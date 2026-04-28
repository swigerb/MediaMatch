using FluentAssertions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Tests.Services;

public sealed class MediaInfoServiceTests
{
    private readonly MediaInfoService _service = new();

    [Fact]
    public async Task GetMediaInfoAsync_NonExistentFile_ReturnsNull()
    {
        var result = await _service.GetMediaInfoAsync(@"C:\nonexistent\video.mkv");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMediaInfoAsync_NonMediaFile_ReturnsNullOrEmpty()
    {
        // ffprobe may not be installed; test graceful degradation
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "not a media file");

            var result = await _service.GetMediaInfoAsync(tempFile);

            // Either null (ffprobe not installed) or result with empty streams (ffprobe can't parse)
            if (result is not null)
            {
                result.FilePath.Should().Be(tempFile);
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void MediaInfoResult_GetAllProperties_IncludesAllStreams()
    {
        var result = new MediaInfoResult
        {
            FilePath = @"C:\test\video.mkv",
            General = new Dictionary<string, string>
            {
                ["Format"] = "Matroska",
                ["Duration"] = "7200.000"
            },
            VideoStreams =
            [
                new Dictionary<string, string>
                {
                    ["CodecName"] = "hevc",
                    ["Width"] = "3840",
                    ["Height"] = "2160"
                }
            ],
            AudioStreams =
            [
                new Dictionary<string, string>
                {
                    ["CodecName"] = "truehd",
                    ["Channels"] = "8"
                }
            ],
            TextStreams =
            [
                new Dictionary<string, string>
                {
                    ["CodecName"] = "subrip",
                    ["Tags.Language"] = "eng"
                }
            ]
        };

        var allProps = result.GetAllProperties().ToList();

        allProps.Should().Contain(kv => kv.Key == "General.Format" && kv.Value == "Matroska");
        allProps.Should().Contain(kv => kv.Key == "Video[0].CodecName" && kv.Value == "hevc");
        allProps.Should().Contain(kv => kv.Key == "Audio[0].CodecName" && kv.Value == "truehd");
        allProps.Should().Contain(kv => kv.Key == "Text[0].CodecName" && kv.Value == "subrip");
        result.StreamCount.Should().Be(3);
    }

    [Fact]
    public void MediaInfoResult_ExportAsText_FormatsCorrectly()
    {
        var result = new MediaInfoResult
        {
            FilePath = @"C:\test\movie.mkv",
            General = new Dictionary<string, string> { ["Format"] = "Matroska" },
            VideoStreams = [new Dictionary<string, string> { ["CodecName"] = "hevc" }],
            AudioStreams = [],
            TextStreams = []
        };

        var text = result.ExportAsText();

        text.Should().Contain("File: C:\\test\\movie.mkv");
        text.Should().Contain("--- General ---");
        text.Should().Contain("Format: Matroska");
        text.Should().Contain("--- Video #1 ---");
        text.Should().Contain("CodecName: hevc");
    }

    [Fact]
    public void MediaInfoResult_Empty_HasZeroStreams()
    {
        var result = new MediaInfoResult();

        result.StreamCount.Should().Be(0);
        result.GetAllProperties().Should().BeEmpty();
    }
}
