using FluentAssertions;
using MediaMatch.Application.Detection;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Tests.Detection;

public sealed class MusicDetectorTests
{
    private readonly MusicDetector _detector = new();

    // ── File Detection ──────────────────────────────────────────
    [Theory]
    [InlineData("song.mp3", true)]
    [InlineData("song.flac", true)]
    [InlineData("song.m4a", true)]
    [InlineData("song.ogg", true)]
    [InlineData("song.wav", true)]
    [InlineData("song.wma", true)]
    [InlineData("song.aac", true)]
    [InlineData("song.opus", true)]
    [InlineData("movie.mkv", false)]
    [InlineData("video.mp4", false)]
    [InlineData("document.txt", false)]
    [InlineData("image.jpg", false)]
    public void IsMusicFile_DetectsCorrectly(string filename, bool expected)
    {
        MusicDetector.IsMusicFile(filename).Should().Be(expected);
    }

    // ── Filename Parsing ────────────────────────────────────────
    [Fact]
    public void DetectFromFilename_ArtistDashTitle_ParsesCorrectly()
    {
        var track = _detector.DetectFromFilename(Path.Combine("C:", "music", "Queen - Bohemian Rhapsody.mp3"));
        track.Should().NotBeNull();
        track!.Artist.Should().Be("Queen");
        track.Title.Should().Be("Bohemian Rhapsody");
    }

    [Fact]
    public void DetectFromFilename_TrackNumberWithDash_ParsesCorrectly()
    {
        var track = _detector.DetectFromFilename(Path.Combine("C:", "music", "01 - Intro.mp3"));
        track.Should().NotBeNull();
        track!.TrackNumber.Should().Be(1);
        track.Title.Should().Be("Intro");
    }

    [Fact]
    public void DetectFromFilename_TrackNumberWithDot_ParsesCorrectly()
    {
        var track = _detector.DetectFromFilename(Path.Combine("C:", "music", "03. Song Title.mp3"));
        track.Should().NotBeNull();
        track!.TrackNumber.Should().Be(3);
    }

    [Fact]
    public void DetectFromFilename_NoArtist_TitleOnly()
    {
        var track = _detector.DetectFromFilename(Path.Combine("C:", "music", "Just A Title.mp3"));
        track.Should().NotBeNull();
        track!.Artist.Should().BeEmpty();
        track.Title.Should().Be("Just A Title");
    }

    [Fact]
    public void DetectFromFilename_EmptyFilename_ReturnsNull()
    {
        var track = _detector.DetectFromFilename(Path.Combine("C:", "music", ".mp3"));
        track.Should().BeNull();
    }

    // ── Featured Artist Extraction ──────────────────────────────
    [Theory]
    [InlineData("Artist feat. Other", "Song", "Artist", "Other")]
    [InlineData("Artist ft. Other", "Song", "Artist", "Other")]
    [InlineData("Artist featuring Other", "Song", "Artist", "Other")]
    [InlineData("Artist", "Song (feat. Guest)", "Artist", "Guest")]
    [InlineData("Artist", "Song [ft. Guest]", "Artist", "Guest")]
    public void ExtractFeaturedArtists_ParsesCorrectly(
        string artist, string title, string expectedArtist, string expectedFeatured)
    {
        var (primary, featured) = MusicDetector.ExtractFeaturedArtists(artist, title);
        primary.Should().Be(expectedArtist);
        featured.Should().Contain(expectedFeatured);
    }

    [Fact]
    public void ExtractFeaturedArtists_MultipleFeatured_SplitsByComma()
    {
        var (primary, featured) = MusicDetector.ExtractFeaturedArtists("Main feat. A, B, C", "Song");
        primary.Should().Be("Main");
        featured.Should().HaveCount(3);
        featured.Should().Contain("A");
        featured.Should().Contain("B");
        featured.Should().Contain("C");
    }

    [Fact]
    public void ExtractFeaturedArtists_NoFeatured_ReturnsEmpty()
    {
        var (primary, featured) = MusicDetector.ExtractFeaturedArtists("Artist", "Song");
        primary.Should().Be("Artist");
        featured.Should().BeEmpty();
    }

    [Fact]
    public void ExtractFeaturedArtists_DeduplicatesResults()
    {
        var (_, featured) = MusicDetector.ExtractFeaturedArtists("Main feat. Guest", "Song (feat. Guest)");
        featured.Should().HaveCount(1);
    }

    // ── Disc Number Detection ───────────────────────────────────
    public static IEnumerable<object?[]> DiscNumberCases()
    {
        yield return new object?[] { Path.Combine("C:", "music", "Album", "CD1", "song.mp3"), 1 };
        yield return new object?[] { Path.Combine("C:", "music", "Album", "CD2", "song.mp3"), 2 };
        yield return new object?[] { Path.Combine("C:", "music", "Album", "Disc 1", "song.mp3"), 1 };
        yield return new object?[] { Path.Combine("C:", "music", "Album", "Disc 3", "song.mp3"), 3 };
        yield return new object?[] { Path.Combine("C:", "music", "Album", "DISC01", "song.mp3"), 1 };
        yield return new object?[] { Path.Combine("C:", "music", "Album", "disk2", "song.mp3"), 2 };
        yield return new object?[] { Path.Combine("C:", "music", "Album", "song.mp3"), null };
    }

    [Theory]
    [MemberData(nameof(DiscNumberCases))]
    public void DetectDiscNumber_ParsesFolderPatterns(string path, int? expected)
    {
        MusicDetector.DetectDiscNumber(path).Should().Be(expected);
    }

    // ── MusicTrack Model ────────────────────────────────────────
    [Fact]
    public void MusicTrack_DisplayArtist_WithFeatured()
    {
        var track = new MusicTrack("Song", "Main",
            FeaturedArtists: new List<string> { "Guest1", "Guest2" });
        track.DisplayArtist.Should().Be("Main feat. Guest1, Guest2");
    }

    [Fact]
    public void MusicTrack_DisplayArtist_WithoutFeatured()
    {
        var track = new MusicTrack("Song", "Solo Artist");
        track.DisplayArtist.Should().Be("Solo Artist");
    }

    [Fact]
    public void MusicTrack_RecordEquality()
    {
        var t1 = new MusicTrack("Song", "Artist", Album: "Album");
        var t2 = new MusicTrack("Song", "Artist", Album: "Album");
        t1.Should().Be(t2);
    }

    [Fact]
    public void DetectFromFilename_FeaturedInTitle_CleanedFromTitle()
    {
        var track = _detector.DetectFromFilename(Path.Combine("C:", "music", "Artist - Song (feat. Guest).mp3"));
        track.Should().NotBeNull();
        track!.Title.Should().Be("Song");
        track.FeaturedArtists.Should().Contain("Guest");
    }
}
