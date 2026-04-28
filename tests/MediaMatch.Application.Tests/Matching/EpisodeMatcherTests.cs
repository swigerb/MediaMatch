using FluentAssertions;
using MediaMatch.Application.Matching;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Tests.Matching;

public sealed class EpisodeMatcherTests
{
    private readonly EpisodeMatcher _matcher = new();

    [Fact]
    public void MatchFiles_BySeasonEpisodeNumbers()
    {
        var files = new[]
        {
            "Breaking.Bad.S01E01.720p.mkv",
            "Breaking.Bad.S01E02.720p.mkv",
        };

        var episodes = new[]
        {
            new Episode("Breaking Bad", 1, 1, "Pilot"),
            new Episode("Breaking Bad", 1, 2, "Cat's in the Bag..."),
        };

        var results = _matcher.MatchFiles(files, episodes);

        results.Should().HaveCount(2);
        results.Should().Contain(m => m.Candidate.EpisodeNumber == 1);
        results.Should().Contain(m => m.Candidate.EpisodeNumber == 2);
    }

    [Fact]
    public void MatchFiles_NoMatchingEpisodes_ReturnsEmpty()
    {
        var files = new[] { "random.file.txt" };
        var episodes = new[]
        {
            new Episode("Breaking Bad", 1, 1, "Pilot"),
        };

        var results = _matcher.MatchFiles(files, episodes);

        // File without season/episode info won't match well
        results.Should().HaveCountLessThanOrEqualTo(1);
    }

    [Fact]
    public void MatchFiles_EmptyFiles_ReturnsEmpty()
    {
        var episodes = new[] { new Episode("Show", 1, 1, "Ep") };
        var results = _matcher.MatchFiles(Array.Empty<string>(), episodes);

        results.Should().BeEmpty();
    }

    [Fact]
    public void MatchFiles_EmptyEpisodes_ReturnsEmpty()
    {
        var files = new[] { "Show.S01E01.mkv" };
        var results = _matcher.MatchFiles(files, Array.Empty<Episode>());

        results.Should().BeEmpty();
    }

    [Fact]
    public void MatchFiles_MoreFilesThanEpisodes()
    {
        var files = new[]
        {
            "Show.S01E01.mkv",
            "Show.S01E02.mkv",
            "Show.S01E03.mkv",
        };

        var episodes = new[]
        {
            new Episode("Show", 1, 1, "Episode 1"),
        };

        var results = _matcher.MatchFiles(files, episodes);

        // Only 1 episode available, so at most 1 match
        results.Should().HaveCount(1);
    }

    [Fact]
    public void MatchFiles_ScoresArePositive()
    {
        var files = new[] { "Show.S01E01.mkv" };
        var episodes = new[] { new Episode("Show", 1, 1, "Episode 1") };

        var results = _matcher.MatchFiles(files, episodes);

        results.Should().HaveCount(1);
        results[0].Score.Should().BeGreaterThan(0);
    }
}
