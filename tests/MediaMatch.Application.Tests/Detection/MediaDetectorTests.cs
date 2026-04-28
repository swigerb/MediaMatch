using FluentAssertions;
using MediaMatch.Application.Detection;
using MediaMatch.Core.Enums;

namespace MediaMatch.Application.Tests.Detection;

public sealed class MediaDetectorTests
{
    private readonly MediaDetector _detector = new();

    [Fact]
    public void DetectMediaType_TvSeries_IdentifiedCorrectly()
    {
        _detector.DetectMediaType("Game.of.Thrones.S01E02.mkv")
            .Should().Be(MediaType.TvSeries);
    }

    [Fact]
    public void DetectMediaType_Movie_IdentifiedCorrectly()
    {
        _detector.DetectMediaType("Inception.2010.1080p.mkv")
            .Should().Be(MediaType.Movie);
    }

    [Fact]
    public void DetectMediaType_Anime_IdentifiedCorrectly()
    {
        _detector.DetectMediaType("[HorribleSubs] Naruto - 42 [720p].mkv")
            .Should().Be(MediaType.Anime);
    }

    [Fact]
    public void DetectMediaType_Music_IdentifiedCorrectly()
    {
        _detector.DetectMediaType("Artist - Song Title.mp3")
            .Should().Be(MediaType.Music);
    }

    [Fact]
    public void DetectMediaType_Unknown_ForNonMediaFile()
    {
        _detector.DetectMediaType("random.document.pdf")
            .Should().Be(MediaType.Unknown);
    }

    [Fact]
    public void DetectMediaType_Subtitle_IdentifiedCorrectly()
    {
        _detector.DetectMediaType("movie.srt")
            .Should().Be(MediaType.Subtitle);
    }

    [Fact]
    public void Detect_ReturnsCompleteDetectionResult()
    {
        var result = _detector.Detect("Game.of.Thrones.S01E02.720p.BluRay.mkv");

        result.Should().NotBeNull();
        result.FilePath.Should().Be("Game.of.Thrones.S01E02.720p.BluRay.mkv");
        result.MediaType.Should().Be(MediaType.TvSeries);
        result.ReleaseInfo.Should().NotBeNull();
        result.Confidence.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Detect_MovieWithManyTokens_HasHighConfidence()
    {
        var result = _detector.Detect("Inception.2010.1080p.BluRay.x264-GROUP.mkv");

        result.Confidence.Should().BeGreaterThanOrEqualTo(0.7f);
    }

    [Fact]
    public void Detect_UnknownFile_HasLowConfidence()
    {
        var result = _detector.Detect("random.pdf");

        result.Confidence.Should().BeLessThan(0.3f);
    }

    [Fact]
    public void DetectBatch_ProcessesMultipleFiles()
    {
        var files = new[] { "Movie.2010.mkv", "Show.S01E01.mkv", "song.mp3" };
        var results = _detector.DetectBatch(files);

        results.Should().HaveCount(3);
        results[0].MediaType.Should().Be(MediaType.Movie);
        results[1].MediaType.Should().Be(MediaType.TvSeries);
        results[2].MediaType.Should().Be(MediaType.Music);
    }
}
