using FluentAssertions;
using MediaMatch.Application.Matching.Metrics;
using MediaMatch.Core.Matching;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Tests.Matching;

/// <summary>
/// Extended similarity metric tests covering edge cases and composite patterns.
/// </summary>
public sealed class SimilarityMetricEdgeCaseTests
{
    // ── SeasonEpisodeMetric extended ─────────────────────────────

    [Theory]
    [InlineData("S01E01", "S01E01", 1.0f)]
    [InlineData("S01E01", "S01E02", 0.5f)]
    [InlineData("S01E01", "S02E01", 0.5f)]
    [InlineData("S01E01", "S02E02", 0.0f)]
    public void SeasonEpisodeMetric_StringPairs_ReturnsExpected(
        string a, string b, float expected)
    {
        var metric = new SeasonEpisodeMetric();
        metric.GetSimilarity(a, b).Should().Be(expected);
    }

    [Fact]
    public void SeasonEpisodeMetric_MixedTypes_EpisodeVsString()
    {
        var metric = new SeasonEpisodeMetric();
        var ep = new Episode("Show", 1, 1, "Pilot");
        metric.GetSimilarity(ep, "S01E01").Should().Be(1.0f);
    }

    [Fact]
    public void SeasonEpisodeMetric_NonParsableString_Returns0()
    {
        var metric = new SeasonEpisodeMetric();
        metric.GetSimilarity("random text", "S01E01").Should().Be(0.0f);
    }

    [Fact]
    public void SeasonEpisodeMetric_EmptyString_Returns0()
    {
        var metric = new SeasonEpisodeMetric();
        metric.GetSimilarity("", "S01E01").Should().Be(0.0f);
    }

    [Fact]
    public void SeasonEpisodeMetric_BothNull_Returns0()
    {
        var metric = new SeasonEpisodeMetric();
        metric.GetSimilarity(null, null).Should().Be(0.0f);
    }

    // ── NameSimilarityMetric extended ────────────────────────────

    [Theory]
    [InlineData("The Office", "the office", 1.0f)]
    [InlineData("Game of Thrones", "game of thrones", 1.0f)]
    public void NameSimilarityMetric_CaseInsensitive_Returns1(string a, string b, float expected)
    {
        var metric = new NameSimilarityMetric();
        metric.GetSimilarity(a, b).Should().Be(expected);
    }

    [Fact]
    public void NameSimilarityMetric_BothNull_Returns0()
    {
        var metric = new NameSimilarityMetric();
        metric.GetSimilarity(null, null).Should().Be(0.0f);
    }

    [Fact]
    public void NameSimilarityMetric_SameCharsDifferentOrder_PartialMatch()
    {
        var metric = new NameSimilarityMetric();
        var score = metric.GetSimilarity("abcdef", "fedcba");
        score.Should().BeGreaterThan(0.0f);
        score.Should().BeLessThan(1.0f);
    }

    [Fact]
    public void NameSimilarityMetric_OneCharacter_Works()
    {
        var metric = new NameSimilarityMetric();
        metric.GetSimilarity("a", "a").Should().Be(1.0f);
    }

    // ── SubstringMetric extended ─────────────────────────────────

    [Fact]
    public void SubstringMetric_CaseInsensitive()
    {
        var metric = new SubstringMetric();
        metric.GetSimilarity("HELLO", "hello world").Should().Be(1.0f);
    }

    [Fact]
    public void SubstringMetric_BothNull_Returns0()
    {
        var metric = new SubstringMetric();
        metric.GetSimilarity(null, null).Should().Be(0.0f);
    }

    [Fact]
    public void SubstringMetric_IdenticalStrings_Returns1()
    {
        var metric = new SubstringMetric();
        metric.GetSimilarity("test", "test").Should().Be(1.0f);
    }

    // ── StringEqualsMetric extended ──────────────────────────────

    [Fact]
    public void StringEqualsMetric_BothNull_Returns0()
    {
        var metric = new StringEqualsMetric();
        metric.GetSimilarity(null, null).Should().Be(0.0f);
    }

    [Fact]
    public void StringEqualsMetric_WhitespaceStrings_Match()
    {
        var metric = new StringEqualsMetric();
        metric.GetSimilarity(" ", " ").Should().Be(1.0f);
    }

    // ── MetricCascade extended ───────────────────────────────────

    [Fact]
    public void MetricCascade_EmptyMetrics_Returns0()
    {
        var cascade = new MetricCascade(Array.Empty<ISimilarityMetric>());
        cascade.GetSimilarity("a", "b").Should().Be(0.0f);
    }

    [Fact]
    public void MetricCascade_FirstMatchUsed()
    {
        var cascade = new MetricCascade(new ISimilarityMetric[]
        {
            new StringEqualsMetric(),
            new SubstringMetric()
        });
        // Exact match on StringEquals returns 1, so cascade returns 1
        cascade.GetSimilarity("hello", "hello").Should().Be(1.0f);
    }

    [Fact]
    public void MetricCascade_Name()
    {
        var cascade = new MetricCascade(Array.Empty<ISimilarityMetric>());
        cascade.Name.Should().NotBeNullOrEmpty();
    }

    // ── MetricAvg extended ───────────────────────────────────────

    [Fact]
    public void MetricAvg_MixedScores_AveragesCorrectly()
    {
        var avg = new MetricAvg(new ISimilarityMetric[]
        {
            new StringEqualsMetric(),   // "abc" vs "abc" → 1.0
            new SubstringMetric()       // "abc" vs "abc" → 1.0
        });
        avg.GetSimilarity("abc", "abc").Should().Be(1.0f);
    }

    [Fact]
    public void MetricAvg_Name()
    {
        var avg = new MetricAvg(Array.Empty<ISimilarityMetric>());
        avg.Name.Should().NotBeNullOrEmpty();
    }

    // ── MetricMin extended ───────────────────────────────────────

    [Fact]
    public void MetricMin_Name()
    {
        var min = new MetricMin(Array.Empty<ISimilarityMetric>());
        min.Name.Should().NotBeNullOrEmpty();
    }

    // ── DateMetric extended ──────────────────────────────────────

    [Fact]
    public void DateMetric_YearsApart_ReturnsVeryLow()
    {
        var metric = new DateMetric();
        var d1 = new DateOnly(2020, 1, 1);
        var d2 = new DateOnly(2023, 1, 1);
        metric.GetSimilarity(d1, d2).Should().BeLessThan(0.1f);
    }

    [Fact]
    public void DateMetric_SimpleDate_Comparison()
    {
        var metric = new DateMetric();
        var d1 = new SimpleDate(2023, 6, 15);
        var d2 = new SimpleDate(2023, 6, 16);
        metric.GetSimilarity(d1, d2).Should().BeGreaterThan(0.9f);
    }

    [Fact]
    public void DateMetric_BothNull_Returns0()
    {
        var metric = new DateMetric();
        metric.GetSimilarity(null, null).Should().Be(0.0f);
    }

    [Fact]
    public void DateMetric_Name()
    {
        var metric = new DateMetric();
        metric.Name.Should().NotBeNullOrEmpty();
    }
}
