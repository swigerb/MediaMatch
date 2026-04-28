using FluentAssertions;
using MediaMatch.Application.Matching;
using MediaMatch.Application.Matching.Metrics;
using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Tests.Matching;

public sealed class BipartiteMatcherTests
{
    [Fact]
    public void PerfectOneToOne_AllMatched()
    {
        var metrics = new ISimilarityMetric[] { new StringEqualsMetric() };
        var matcher = new BipartiteMatcher<string, string>(metrics, threshold: 0.5f);

        var values = new[] { "A", "B", "C" };
        var candidates = new[] { "C", "A", "B" };

        var results = matcher.Match(values, candidates, v => v, c => c);

        results.Should().HaveCount(3);
        results.Should().Contain(m => m.Value == "A" && m.Candidate == "A");
        results.Should().Contain(m => m.Value == "B" && m.Candidate == "B");
        results.Should().Contain(m => m.Value == "C" && m.Candidate == "C");
    }

    [Fact]
    public void NoMatchesAboveThreshold_ReturnsEmpty()
    {
        var metrics = new ISimilarityMetric[] { new StringEqualsMetric() };
        var matcher = new BipartiteMatcher<string, string>(metrics, threshold: 0.5f);

        var values = new[] { "A", "B" };
        var candidates = new[] { "X", "Y" };

        var results = matcher.Match(values, candidates, v => v, c => c);

        results.Should().BeEmpty();
    }

    [Fact]
    public void MoreValues_ThanCandidates_UnmatchedValuesDropped()
    {
        var metrics = new ISimilarityMetric[] { new StringEqualsMetric() };
        var matcher = new BipartiteMatcher<string, string>(metrics, threshold: 0.5f);

        var values = new[] { "A", "B", "C" };
        var candidates = new[] { "A" };

        var results = matcher.Match(values, candidates, v => v, c => c);

        results.Should().HaveCount(1);
        results[0].Value.Should().Be("A");
    }

    [Fact]
    public void MoreCandidates_ThanValues_ExtraCandidatesIgnored()
    {
        var metrics = new ISimilarityMetric[] { new StringEqualsMetric() };
        var matcher = new BipartiteMatcher<string, string>(metrics, threshold: 0.5f);

        var values = new[] { "A" };
        var candidates = new[] { "A", "B", "C" };

        var results = matcher.Match(values, candidates, v => v, c => c);

        results.Should().HaveCount(1);
    }

    [Fact]
    public void EmptyValues_ReturnsEmpty()
    {
        var metrics = new ISimilarityMetric[] { new StringEqualsMetric() };
        var matcher = new BipartiteMatcher<string, string>(metrics, threshold: 0.5f);

        var results = matcher.Match(
            Array.Empty<string>(),
            new[] { "A" },
            v => v,
            c => c);

        results.Should().BeEmpty();
    }

    [Fact]
    public void EmptyCandidates_ReturnsEmpty()
    {
        var metrics = new ISimilarityMetric[] { new StringEqualsMetric() };
        var matcher = new BipartiteMatcher<string, string>(metrics, threshold: 0.5f);

        var results = matcher.Match(
            new[] { "A" },
            Array.Empty<string>(),
            v => v,
            c => c);

        results.Should().BeEmpty();
    }

    [Fact]
    public void AmbiguousMatching_GreedyPicksBestFirst()
    {
        var metrics = new ISimilarityMetric[] { new SubstringMetric() };
        var matcher = new BipartiteMatcher<string, string>(metrics, threshold: 0.5f);

        var values = new[] { "Game of Thrones", "Game" };
        var candidates = new[] { "Game of Thrones S01", "Game Night" };

        var results = matcher.Match(values, candidates, v => v, c => c);

        // Both values contain "Game", but greedy matching should assign each value to one candidate
        results.Should().HaveCount(2);
    }

    [Fact]
    public void ScoreReflectsMetricOutput()
    {
        var metrics = new ISimilarityMetric[] { new StringEqualsMetric() };
        var matcher = new BipartiteMatcher<string, string>(metrics, threshold: 0.5f);

        var results = matcher.Match(
            new[] { "hello" },
            new[] { "hello" },
            v => v,
            c => c);

        results.Should().HaveCount(1);
        results[0].Score.Should().Be(1.0f);
    }
}
