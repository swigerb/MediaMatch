using FluentAssertions;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;

namespace MediaMatch.App.Tests.Models;

public sealed class CoreModelTests
{
    [Fact]
    public void Movie_RecordEquality_WorksCorrectly()
    {
        var a = new Movie("Inception", 2010, TmdbId: 27205);
        var b = new Movie("Inception", 2010, TmdbId: 27205);
        var c = new Movie("Inception", 2011, TmdbId: 27205);

        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void Episode_DefaultOptionalValues_AreNull()
    {
        var ep = new Episode("Breaking Bad", 1, 1, "Pilot");

        ep.AbsoluteNumber.Should().BeNull();
        ep.Special.Should().BeNull();
        ep.AirDate.Should().BeNull();
        ep.SeriesId.Should().BeNull();
        ep.SortOrder.Should().Be(SortOrder.Airdate);
    }

    [Fact]
    public void SearchResult_ToString_ReturnsName()
    {
        var result = new SearchResult("Breaking Bad", 81189);

        result.ToString().Should().Be("Breaking Bad");
    }

    [Fact]
    public void SimpleDate_TryParse_ValidDate_ReturnsParsed()
    {
        var date = SimpleDate.TryParse("2023-06-15");

        date.Should().NotBeNull();
        date!.Value.Year.Should().Be(2023);
        date.Value.Month.Should().Be(6);
        date.Value.Day.Should().Be(15);
    }

    [Fact]
    public void SimpleDate_TryParse_NullOrEmpty_ReturnsNull()
    {
        SimpleDate.TryParse(null).Should().BeNull();
        SimpleDate.TryParse("").Should().BeNull();
    }

    [Fact]
    public void SimpleDate_TryParse_InvalidDate_ReturnsNull()
    {
        SimpleDate.TryParse("not-a-date").Should().BeNull();
        SimpleDate.TryParse("9999-99-99").Should().BeNull();
    }

    [Fact]
    public void SimpleDate_CompareTo_OrdersCorrectly()
    {
        var earlier = new SimpleDate(2020, 1, 1);
        var later = new SimpleDate(2021, 6, 15);

        earlier.CompareTo(later).Should().BeNegative();
        later.CompareTo(earlier).Should().BePositive();
        earlier.CompareTo(earlier).Should().Be(0);
    }

    [Fact]
    public void SimpleDate_ToDateOnly_ConvertsCorrectly()
    {
        var simple = new SimpleDate(2023, 12, 25);

        var dateOnly = simple.ToDateOnly();

        dateOnly.Should().NotBeNull();
        dateOnly!.Value.Year.Should().Be(2023);
        dateOnly.Value.Month.Should().Be(12);
        dateOnly.Value.Day.Should().Be(25);
    }

    [Fact]
    public void MatchResult_NoMatch_HasZeroConfidence()
    {
        var result = MatchResult.NoMatch(MediaType.Movie);

        result.Confidence.Should().Be(0f);
        result.ProviderSource.Should().Be("none");
        result.MediaType.Should().Be(MediaType.Movie);
    }

    [Fact]
    public void MatchResult_IsMatch_TrueWhenConfidencePositive()
    {
        var match = new MatchResult(MediaType.TvSeries, 0.85f, "tmdb");
        var noMatch = new MatchResult(MediaType.Movie, 0f, "none");

        match.IsMatch.Should().BeTrue();
        noMatch.IsMatch.Should().BeFalse();
    }

    [Fact]
    public void Artwork_TypeEnum_HasExpectedValues()
    {
        Enum.IsDefined(typeof(ArtworkType), ArtworkType.Poster).Should().BeTrue();
        Enum.IsDefined(typeof(ArtworkType), ArtworkType.Banner).Should().BeTrue();
        Enum.IsDefined(typeof(ArtworkType), ArtworkType.Fanart).Should().BeTrue();
    }

    [Fact]
    public void Person_PositionalConstruction_SetsProperties()
    {
        var person = new Person("John Doe", Character: "Hero", Department: "Acting", TmdbId: 12345);

        person.Name.Should().Be("John Doe");
        person.Character.Should().Be("Hero");
        person.Department.Should().Be("Acting");
        person.TmdbId.Should().Be(12345);
        person.Job.Should().BeNull();
        person.ProfileUrl.Should().BeNull();
        person.Order.Should().BeNull();
    }

    [Fact]
    public void MovieInfo_PositionalConstruction_SetsProperties()
    {
        var genres = new List<string> { "Action", "Sci-Fi" };
        var cast = new List<Person> { new("Actor One") };
        var crew = new List<Person> { new("Director One", Job: "Director") };

        var info = new MovieInfo(
            Name: "Inception",
            Year: 2010,
            TmdbId: 27205,
            ImdbId: "tt1375666",
            Overview: "A mind-bending thriller",
            Tagline: "Your mind is the scene of the crime",
            PosterUrl: "https://example.com/poster.jpg",
            Rating: 8.8,
            Runtime: 148,
            Certification: "PG-13",
            Genres: genres,
            Cast: cast,
            Crew: crew);

        info.Name.Should().Be("Inception");
        info.Year.Should().Be(2010);
        info.TmdbId.Should().Be(27205);
        info.ImdbId.Should().Be("tt1375666");
        info.Runtime.Should().Be(148);
        info.Genres.Should().BeEquivalentTo(genres);
        info.Cast.Should().HaveCount(1);
        info.Crew.Should().HaveCount(1);
        info.OriginalLanguage.Should().BeNull();
        info.Revenue.Should().BeNull();
        info.Budget.Should().BeNull();
        info.Collection.Should().BeNull();
    }

    [Fact]
    public void SeriesInfo_PositionalConstruction_SetsProperties()
    {
        var genres = new List<string> { "Drama", "Crime" };

        var info = new SeriesInfo(
            Name: "Breaking Bad",
            Id: "81189",
            Overview: "A chemistry teacher turns to crime",
            Network: "AMC",
            Status: "Ended",
            Rating: 9.5,
            Runtime: 47,
            Genres: genres,
            PosterUrl: "https://example.com/poster.jpg",
            StartDate: new SimpleDate(2008, 1, 20));

        info.Name.Should().Be("Breaking Bad");
        info.Id.Should().Be("81189");
        info.Network.Should().Be("AMC");
        info.Status.Should().Be("Ended");
        info.Genres.Should().BeEquivalentTo(genres);
        info.StartDate.Should().NotBeNull();
        info.ImdbId.Should().BeNull();
        info.TmdbId.Should().BeNull();
        info.Language.Should().BeNull();
        info.AliasNames.Should().BeNull();
    }
}
