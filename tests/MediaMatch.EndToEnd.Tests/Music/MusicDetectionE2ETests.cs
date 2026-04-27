using FluentAssertions;
using MediaMatch.Application.Detection;
using MediaMatch.Application.Expressions;
using MediaMatch.Application.Services;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using MediaMatch.EndToEnd.Tests.Fixtures;
using Moq;

namespace MediaMatch.EndToEnd.Tests.Music;

/// <summary>
/// E2E: Music detection → tag extraction → mock MusicBrainz match → rename generation.
/// </summary>
public class MusicDetectionE2ETests
{
    private readonly ScribanExpressionEngine _engine = new();

    // ── Music file detection ──────────────────────────────────────────────

    [Theory]
    [InlineData("track01.mp3")]
    [InlineData("album_track.flac")]
    [InlineData("song.m4a")]
    [InlineData("audio.ogg")]
    [InlineData("music.wav")]
    [InlineData("sound.wma")]
    [InlineData("opus_file.opus")]
    public void MusicDetector_KnownExtensions_DetectedAsMusicFile(string filename)
    {
        MusicDetector.IsMusicFile(filename).Should().BeTrue();
    }

    [Theory]
    [InlineData("video.mkv")]
    [InlineData("movie.mp4")]
    [InlineData("document.pdf")]
    [InlineData("image.jpg")]
    public void MusicDetector_NonMusicExtensions_NotDetectedAsMusicFile(string filename)
    {
        MusicDetector.IsMusicFile(filename).Should().BeFalse();
    }

    // ── Filename-based music parsing ──────────────────────────────────────

    [Fact]
    public void MusicTrack_Bindings_AllMusicTokensAvailable()
    {
        var track = new MusicTrack(
            Title: "Bohemian Rhapsody",
            Artist: "Queen",
            Album: "A Night at the Opera",
            AlbumArtist: "Queen",
            TrackNumber: 11,
            DiscNumber: 1,
            TotalDiscs: 1,
            Genre: "Rock",
            Year: 1975);
        var bindings = MediaBindings.ForMusic(track, "11_bohemian_rhapsody.flac");

        _engine.Evaluate("{n} - {album} - {track} - {t}", bindings)
            .Should().Be("Queen - A Night at the Opera - 11 - Bohemian Rhapsody");
    }

    [Fact]
    public void MusicTrack_WithFeaturedArtists_DisplayArtistIncludesFeat()
    {
        var track = new MusicTrack(
            Title: "Empire State of Mind",
            Artist: "Jay-Z",
            Album: "The Blueprint 3",
            AlbumArtist: "Jay-Z",
            TrackNumber: 1,
            FeaturedArtists: ["Alicia Keys"]);

        track.DisplayArtist.Should().Be("Jay-Z feat. Alicia Keys");
    }

    [Fact]
    public void MusicTrack_NoFeaturedArtists_DisplayArtistIsArtist()
    {
        var track = new MusicTrack(
            Title: "Bohemian Rhapsody",
            Artist: "Queen",
            TrackNumber: 11);

        track.DisplayArtist.Should().Be("Queen");
    }

    // ── Multi-disc handling ───────────────────────────────────────────────

    [Fact]
    public void MusicTrack_MultiDisc_DiscNumberInTemplate()
    {
        var track = new MusicTrack(
            Title: "Comfortably Numb",
            Artist: "Pink Floyd",
            Album: "The Wall",
            AlbumArtist: "Pink Floyd",
            TrackNumber: 6,
            DiscNumber: 2,
            TotalDiscs: 2,
            Year: 1979);
        var bindings = MediaBindings.ForMusic(track, "track06.flac");

        var result = _engine.Evaluate("{albumartist}/{album}/{disc}-{track} {title}{extension}", bindings);

        result.Should().Be("Pink Floyd/The Wall/2-6 Comfortably Numb.flac");
    }

    [Fact]
    public void MusicTrack_SingleDisc_DiscOne()
    {
        var track = new MusicTrack(
            Title: "Thriller",
            Artist: "Michael Jackson",
            Album: "Thriller",
            TrackNumber: 1,
            DiscNumber: 1);

        track.DiscNumber.Should().Be(1);
    }

    // ── Mock MusicBrainz provider flow ────────────────────────────────────

    [Fact]
    public async Task MusicProvider_SearchAndMatch_ReturnsMusicTrack()
    {
        var musicProvider = new Mock<IMusicProvider>();
        musicProvider.Setup(p => p.Name).Returns("MusicBrainz");
        musicProvider
            .Setup(p => p.SearchAsync("Queen", "Bohemian Rhapsody", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MusicTrack>
            {
                new("Bohemian Rhapsody", "Queen", "A Night at the Opera",
                    TrackNumber: 11, Year: 1975, MusicBrainzId: "abc123")
            });

        var results = await musicProvider.Object.SearchAsync("Queen", "Bohemian Rhapsody", CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].Title.Should().Be("Bohemian Rhapsody");
        results[0].Artist.Should().Be("Queen");
        results[0].MusicBrainzId.Should().Be("abc123");
    }

    [Fact]
    public async Task MusicProvider_NoMatch_ReturnsEmpty()
    {
        var musicProvider = new Mock<IMusicProvider>();
        musicProvider.Setup(p => p.Name).Returns("MusicBrainz");
        musicProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MusicTrack>());

        var results = await musicProvider.Object.SearchAsync("unknown", "track", CancellationToken.None);

        results.Should().BeEmpty();
    }

    // ── Music rename template ─────────────────────────────────────────────

    [Theory]
    [InlineData("{albumartist}/{album}/{track} {title}{extension}", "Queen/A Night at the Opera/11 Bohemian Rhapsody.flac")]
    [InlineData("{artist} - {album} ({y})", "Queen - A Night at the Opera (1975)")]
    [InlineData("{n} - {t}", "Queen - Bohemian Rhapsody")]
    public void MusicRenameTemplates_ProduceExpectedOutput(string template, string expected)
    {
        var track = new MusicTrack(
            Title: "Bohemian Rhapsody",
            Artist: "Queen",
            Album: "A Night at the Opera",
            AlbumArtist: "Queen",
            TrackNumber: 11,
            Year: 1975);
        var bindings = MediaBindings.ForMusic(track, "track11.flac");

        var result = _engine.Evaluate(template, bindings);

        result.Should().Be(expected);
    }

    // ── AcoustID fingerprint flow ─────────────────────────────────────────

    [Fact]
    public async Task AcoustIdProvider_Fingerprint_ReturnsMatchedTrack()
    {
        var acoustId = new Mock<IMusicProvider>();
        acoustId.Setup(p => p.Name).Returns("AcoustID");
        acoustId
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MusicTrack>
            {
                new("Hotel California", "Eagles", "Hotel California",
                    TrackNumber: 1, Year: 1977, MusicBrainzId: "xyz789")
            });

        var results = await acoustId.Object.SearchAsync("Eagles", "Hotel California", CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].Title.Should().Be("Hotel California");
        results[0].Artist.Should().Be("Eagles");
    }

    // ── Featuring artist extraction via template ───────────────────────────

    [Fact]
    public void Music_FeaturingBinding_RenderedInTemplate()
    {
        var track = new MusicTrack(
            Title: "Old Town Road",
            Artist: "Lil Nas X",
            Album: "7",
            AlbumArtist: "Lil Nas X",
            TrackNumber: 1,
            FeaturedArtists: ["Billy Ray Cyrus"]);
        var bindings = MediaBindings.ForMusic(track);

        var featuring = _engine.Evaluate("{featuring}", bindings);
        featuring.Should().Be("Billy Ray Cyrus");
    }
}
