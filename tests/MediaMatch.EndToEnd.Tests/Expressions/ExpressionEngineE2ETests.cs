using FluentAssertions;
using MediaMatch.Application.Expressions;
using MediaMatch.Core.Models;

namespace MediaMatch.EndToEnd.Tests.Expressions;

/// <summary>
/// E2E: Full template rendering with all binding tokens — verifies complete path generation.
/// </summary>
public sealed class ExpressionEngineE2ETests
{
    private readonly ScribanExpressionEngine _engine = new();

    // ── Episode bindings ──────────────────────────────────────────────────

    [Fact]
    public void Episode_AllCoreTokens_RenderedCorrectly()
    {
        var episode = new Episode("Breaking Bad", 1, 2, "Cat's in the Bag...");
        var bindings = MediaBindings.ForEpisode(episode);

        var result = _engine.Evaluate("{n} - {s00e00} - {t}", bindings);

        result.Should().Be("Breaking Bad - S01E02 - Cat's in the Bag...");
    }

    [Fact]
    public void Episode_SeasonPaddedTokens()
    {
        var episode = new Episode("The Wire", 3, 7, "Back Burners");
        var bindings = MediaBindings.ForEpisode(episode);

        _engine.Evaluate("{s00}", bindings).Should().Be("03");
        _engine.Evaluate("{e00}", bindings).Should().Be("07");
        _engine.Evaluate("{s00e00}", bindings).Should().Be("S03E07");
        _engine.Evaluate("{sxe}", bindings).Should().Be("3x07");
    }

    [Fact]
    public void Episode_JellyfinToken_ProducesJellyfinNaming()
    {
        var episode = new Episode("Breaking Bad", 1, 2, "Cat's in the Bag...");
        var bindings = MediaBindings.ForEpisode(episode);

        var result = _engine.Evaluate("{jellyfin}", bindings);

        result.Should().Be("Breaking Bad - S01E02 - Cat's in the Bag...");
    }

    [Fact]
    public void Episode_MultiEpisode_RangeInBinding()
    {
        var episode = new Episode("Firefly", 1, 1, "Serenity");
        var bindings = MediaBindings.ForEpisode(episode, endEpisode: 2);

        var result = _engine.Evaluate("{s00e00}", bindings);

        result.Should().Be("S01E01-E02");
    }

    [Fact]
    public void Episode_MultiEpisode_JellyfinRangeNaming()
    {
        var episode = new Episode("Firefly", 1, 1, "Serenity");
        var bindings = MediaBindings.ForEpisode(episode, endEpisode: 2);

        var result = _engine.Evaluate("{jellyfin}", bindings);

        result.Should().Be("Firefly S01E01-S01E02");
    }

    // ── Technical info bindings ───────────────────────────────────────────

    [Fact]
    public void Episode_TechnicalBindings_AllRendered()
    {
        var episode = new Episode("Breaking Bad", 1, 1, "Pilot");
        var techInfo = new MediaTechnicalInfo(
            AudioChannels: "5.1",
            DolbyVision: "DoVi Profile 5",
            HdrFormat: "HDR10",
            Resolution: "1080p",
            BitDepth: "10bit",
            VideoCodec: "HEVC",
            AudioCodec: "DTS");
        var bindings = MediaBindings.ForEpisode(episode, techInfo: techInfo);

        _engine.Evaluate("{resolution}", bindings).Should().Be("1080p");
        _engine.Evaluate("{bitdepth}", bindings).Should().Be("10bit");
        _engine.Evaluate("{hdr}", bindings).Should().Be("HDR10");
        _engine.Evaluate("{dovi}", bindings).Should().Be("DoVi Profile 5");
        _engine.Evaluate("{acf}", bindings).Should().Be("5.1");
    }

    [Fact]
    public void Episode_ComplexTemplate_WithTechnicalInfo()
    {
        var episode = new Episode("Breaking Bad", 1, 1, "Pilot");
        var techInfo = new MediaTechnicalInfo(
            AudioChannels: "7.1",
            DolbyVision: null,
            HdrFormat: "HDR10",
            Resolution: "4K",
            BitDepth: "10bit",
            VideoCodec: "HEVC",
            AudioCodec: "TrueHD");
        var bindings = MediaBindings.ForEpisode(episode, techInfo: techInfo);

        var result = _engine.Evaluate("{n} - {s00e00} [{resolution} {hdr} {acf}]", bindings);

        result.Should().Be("Breaking Bad - S01E01 [4K HDR10 7.1]");
    }

    // ── Movie bindings ────────────────────────────────────────────────────

    [Fact]
    public void Movie_StandardTokens_RenderedCorrectly()
    {
        var movie = new Movie("Inception", 2010, TmdbId: 27205);
        var bindings = MediaBindings.ForMovie(movie);

        _engine.Evaluate("{n} ({y})", bindings).Should().Be("Inception (2010)");
        _engine.Evaluate("{t}", bindings).Should().Be("Inception");
    }

    [Fact]
    public void Movie_JellyfinToken_ProducesJellyfinNaming()
    {
        var movie = new Movie("The Dark Knight", 2008, TmdbId: 155);
        var bindings = MediaBindings.ForMovie(movie);

        var result = _engine.Evaluate("{jellyfin}", bindings);

        result.Should().Be("The Dark Knight (2008)");
    }

    [Fact]
    public void Movie_WithMovieInfo_ExtraTokens()
    {
        var movie = new Movie("Inception", 2010, TmdbId: 27205);
        var movieInfo = new MovieInfo(
            "Inception", 2010, 27205,
            "tt1375666",
            Overview: null,
            Tagline: "Your mind is the scene of the crime",
            PosterUrl: null,
            Rating: 8.8,
            Runtime: null,
            Certification: "PG-13",
            Genres: ["Action", "Sci-Fi"],
            Cast: [new Person("Leonardo DiCaprio", "Dom Cobb", "Acting", null)],
            Crew: [new Person("Christopher Nolan", null, "Directing", "Director")]);
        var bindings = MediaBindings.ForMovie(movie, movieInfo);

        _engine.Evaluate("{imdb}", bindings).Should().Be("tt1375666");
        _engine.Evaluate("{genre}", bindings).Should().Be("Action");
        _engine.Evaluate("{director}", bindings).Should().Be("Christopher Nolan");
        _engine.Evaluate("{certification}", bindings).Should().Be("PG-13");
    }

    // ── Music bindings ────────────────────────────────────────────────────

    [Fact]
    public void Music_AllMusicTokens_RenderedCorrectly()
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
            Year: 1975,
            FeaturedArtists: null);
        var bindings = MediaBindings.ForMusic(track);

        _engine.Evaluate("{n}", bindings).Should().Be("Queen");
        _engine.Evaluate("{t}", bindings).Should().Be("Bohemian Rhapsody");
        _engine.Evaluate("{artist}", bindings).Should().Be("Queen");
        _engine.Evaluate("{album}", bindings).Should().Be("A Night at the Opera");
        _engine.Evaluate("{genre}", bindings).Should().Be("Rock");
        _engine.Evaluate("{y}", bindings).Should().Be("1975");
    }

    [Fact]
    public void Music_MusicRenameTemplate_ProducesCorrectPath()
    {
        var track = new MusicTrack(
            Title: "Comfortably Numb",
            Artist: "Pink Floyd",
            Album: "The Wall",
            AlbumArtist: "Pink Floyd",
            TrackNumber: 6,
            DiscNumber: 2,
            TotalDiscs: 2,
            Genre: "Progressive Rock",
            Year: 1979,
            FeaturedArtists: null);
        var bindings = MediaBindings.ForMusic(track, filePath: "track06.flac");

        var result = _engine.Evaluate("{albumartist}/{album}/{track} {title}{extension}", bindings);

        result.Should().Be("Pink Floyd/The Wall/6 Comfortably Numb.flac");
    }

    // ── Helper functions ──────────────────────────────────────────────────

    [Fact]
    public void ExpressionHelper_PadFunction_Works()
    {
        var episode = new Episode("Show", 1, 3, "Episode Three");
        var bindings = MediaBindings.ForEpisode(episode);

        var result = _engine.Evaluate("{mm.pad e 3}", bindings);

        result.Should().Be("003");
    }

    [Fact]
    public void ExpressionHelper_CleanFilename_RemovesInvalidChars()
    {
        var movie = new Movie("Movie: The Sequel", 2023, TmdbId: 1);
        var bindings = MediaBindings.ForMovie(movie);

        // Clean function strips the colon which is invalid in Windows filenames
        var result = _engine.Evaluate("{mm.clean_filename n}", bindings);

        result.Should().NotContain(":");
    }

    [Fact]
    public void ExpressionHelper_Coalesce_ReturnsFirstNonEmpty()
    {
        var episode = new Episode("Show", 1, 1, "");
        var bindings = MediaBindings.ForEpisode(episode);

        // t is empty, so coalesce returns fallback
        var result = _engine.Evaluate("{mm.coalesce t 'Unknown Episode'}", bindings);

        result.Should().Be("Unknown Episode");
    }

    // ── Validation ────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidExpression_ReturnsTrue()
    {
        var valid = _engine.Validate("{n} - {s00e00} - {t}", out var error);

        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Validate_InvalidExpression_ReturnsFalse()
    {
        var valid = _engine.Validate("{{{broken", out var error);

        valid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FileBotSyntaxConversion_SingleBraces_ConvertedToDouble()
    {
        var result = ScribanExpressionEngine.ConvertFromFileBotSyntax("{n} ({y})");

        result.Should().Be("{{n}} ({{y}})");
    }
}
