using FluentAssertions;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;

namespace MediaMatch.Core.Tests.Models;

/// <summary>
/// Extended tests for core models: MatchResult, FileOrganizationResult, 
/// Episode, Movie, SearchResult, Person, and related records.
/// </summary>
public sealed class CoreModelExtendedTests
{
    // ── MatchResult ──────────────────────────────────────────────

    [Fact]
    public void MatchResult_NoMatch_IsMatchFalse()
    {
        var result = MatchResult.NoMatch(MediaType.Movie);
        result.IsMatch.Should().BeFalse();
        result.MediaType.Should().Be(MediaType.Movie);
    }

    [Fact]
    public void MatchResult_WithConfidence_IsMatchTrue()
    {
        var result = new MatchResult(MediaType.TvSeries, 0.85f, "TMDb");
        result.IsMatch.Should().BeTrue();
        result.Confidence.Should().Be(0.85f);
        result.ProviderSource.Should().Be("TMDb");
    }

    [Fact]
    public void MatchResult_ZeroConfidence_IsMatchFalse()
    {
        var result = new MatchResult(MediaType.Movie, 0.0f, "None");
        result.IsMatch.Should().BeFalse();
    }

    [Fact]
    public void MatchResult_WithEpisode_HasEpisodeData()
    {
        var ep = new Episode("Show", 1, 1, "Pilot");
        var result = new MatchResult(MediaType.TvSeries, 0.90f, "TVDb", Episode: ep);
        result.Episode.Should().NotBeNull();
        result.Episode!.Title.Should().Be("Pilot");
    }

    [Fact]
    public void MatchResult_WithMovie_HasMovieData()
    {
        var movie = new Movie("Test Movie", 2024);
        var result = new MatchResult(MediaType.Movie, 0.88f, "TMDb", Movie: movie);
        result.Movie.Should().NotBeNull();
        result.Movie!.Name.Should().Be("Test Movie");
    }

    // ── FileOrganizationResult ───────────────────────────────────

    [Fact]
    public void FileOrganizationResult_Failed_ReturnsFailedResult()
    {
        var result = FileOrganizationResult.Failed("test.mkv", "No match found");
        result.Success.Should().BeFalse();
        result.OriginalPath.Should().Be("test.mkv");
        result.Warnings.Should().ContainSingle().Which.Should().Contain("No match found");
    }

    [Fact]
    public void FileOrganizationResult_Success_HasAllProperties()
    {
        var result = new FileOrganizationResult(
            "original.mkv", "new/path.mkv", 0.95f, MediaType.Movie,
            new List<string>(), true);
        result.Success.Should().BeTrue();
        result.NewPath.Should().Be("new/path.mkv");
        result.MatchConfidence.Should().Be(0.95f);
        result.MediaType.Should().Be(MediaType.Movie);
    }

    [Fact]
    public void FileOrganizationResult_WithWarnings_PreservesWarnings()
    {
        var warnings = new List<string> { "Low confidence", "Year mismatch" };
        var result = new FileOrganizationResult(
            "test.mkv", "dest/test.mkv", 0.65f, MediaType.TvSeries,
            warnings, true);
        result.Warnings.Should().HaveCount(2);
    }

    // ── Episode model ────────────────────────────────────────────

    [Fact]
    public void Episode_AllProperties_SetCorrectly()
    {
        var airDate = new SimpleDate(2023, 6, 15);
        var ep = new Episode("Breaking Bad", 5, 16, "Felina",
            AbsoluteNumber: 62, AirDate: airDate, SeriesId: "81189");

        ep.SeriesName.Should().Be("Breaking Bad");
        ep.Season.Should().Be(5);
        ep.EpisodeNumber.Should().Be(16);
        ep.Title.Should().Be("Felina");
        ep.AbsoluteNumber.Should().Be(62);
        ep.AirDate.Should().Be(airDate);
        ep.SeriesId.Should().Be("81189");
    }

    [Fact]
    public void Episode_DefaultSortOrder_IsAirdate()
    {
        var ep = new Episode("Show", 1, 1, "Pilot");
        ep.SortOrder.Should().Be(SortOrder.Airdate);
    }

    // ── Movie model ──────────────────────────────────────────────

    [Fact]
    public void Movie_OptionalProperties_DefaultToNull()
    {
        var movie = new Movie("Test", 2024);
        movie.TmdbId.Should().BeNull();
        movie.ImdbId.Should().BeNull();
        movie.Language.Should().BeNull();
    }

    [Fact]
    public void Movie_WithAllOptionals_SetsCorrectly()
    {
        var movie = new Movie("Test", 2024, TmdbId: 12345, ImdbId: "tt0067890", Language: "en");
        movie.TmdbId.Should().Be(12345);
        movie.ImdbId.Should().Be("tt0067890");
        movie.Language.Should().Be("en");
    }

    // ── SearchResult model ───────────────────────────────────────

    [Fact]
    public void SearchResult_WithAliasNames()
    {
        var aliases = new List<string> { "Alt Name 1", "Alt Name 2" };
        var sr = new SearchResult("Show", 42, aliases);
        sr.Name.Should().Be("Show");
        sr.Id.Should().Be(42);
        sr.AliasNames.Should().HaveCount(2);
    }

    [Fact]
    public void SearchResult_NoAliases_DefaultsToNull()
    {
        var sr = new SearchResult("Show", 1);
        sr.AliasNames.Should().BeNull();
    }

    // ── Person model ─────────────────────────────────────────────

    [Fact]
    public void Person_Actor_HasCharacter()
    {
        var person = new Person("Bryan Cranston", Character: "Walter White");
        person.Name.Should().Be("Bryan Cranston");
        person.Character.Should().Be("Walter White");
    }

    [Fact]
    public void Person_Crew_HasDepartmentAndJob()
    {
        var person = new Person("Vince Gilligan",
            Department: "Directing", Job: "Director");
        person.Department.Should().Be("Directing");
        person.Job.Should().Be("Director");
    }

    // ── MatchSuggestion model ────────────────────────────────────

    [Fact]
    public void MatchSuggestion_Properties_SetCorrectly()
    {
        var suggestion = new MatchSuggestion("TMDb", 0.85, "Breaking Bad",
            2008, "Crime Drama", "1396");
        suggestion.ProviderName.Should().Be("TMDb");
        suggestion.Confidence.Should().Be(0.85);
        suggestion.Title.Should().Be("Breaking Bad");
        suggestion.Year.Should().Be(2008);
        suggestion.MetadataSummary.Should().Be("Crime Drama");
        suggestion.ProviderId.Should().Be("1396");
    }

    // ── RenamePattern model ──────────────────────────────────────

    [Fact]
    public void RenamePattern_Properties_SetCorrectly()
    {
        var pattern = new RenamePattern(
            "{{series_name}} S{{season}}E{{episode}}",
            "Standard episode format",
            "Breaking Bad S01E01");
        pattern.Template.Should().NotBeEmpty();
        pattern.Description.Should().NotBeEmpty();
        pattern.ExampleOutput.Should().Contain("Breaking Bad");
    }

    // ── MediaTechnicalInfo ───────────────────────────────────────

    [Fact]
    public void MediaTechnicalInfo_Unknown_HasDefaults()
    {
        var info = MediaTechnicalInfo.Unknown;
        info.Resolution.Should().NotBeNull();
        info.VideoCodec.Should().NotBeNull();
        info.AudioCodec.Should().NotBeNull();
    }

    [Fact]
    public void MediaTechnicalInfo_AllProperties_SetCorrectly()
    {
        var info = new MediaTechnicalInfo(
            "7.1 Atmos", "DV", "HDR10", "UHD", "10bit", "HEVC", "TrueHD");
        info.AudioChannels.Should().Be("7.1 Atmos");
        info.DolbyVision.Should().Be("DV");
        info.HdrFormat.Should().Be("HDR10");
        info.Resolution.Should().Be("UHD");
        info.BitDepth.Should().Be("10bit");
        info.VideoCodec.Should().Be("HEVC");
        info.AudioCodec.Should().Be("TrueHD");
    }

    // ── Artwork model ────────────────────────────────────────────

    [Fact]
    public void Artwork_Properties_SetCorrectly()
    {
        var art = new Artwork("https://example.com/poster.jpg", ArtworkType.Poster,
            Language: "en", Rating: 8.5, Width: 1920, Height: 1080);
        art.Url.Should().Contain("poster");
        art.Type.Should().Be(ArtworkType.Poster);
        art.Language.Should().Be("en");
    }

    // ── UndoEntry model ──────────────────────────────────────────

    [Fact]
    public void UndoEntry_Properties_SetCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new UndoEntry("original.mkv", "new.mkv", now, MediaType.Movie);
        entry.OriginalPath.Should().Be("original.mkv");
        entry.NewPath.Should().Be("new.mkv");
        entry.Timestamp.Should().Be(now);
        entry.MediaType.Should().Be(MediaType.Movie);
    }
}
