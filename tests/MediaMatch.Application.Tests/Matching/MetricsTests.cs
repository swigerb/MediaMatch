using FluentAssertions;
using MediaMatch.Application.Matching.Metrics;
using MediaMatch.Core.Matching;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Tests.Matching;

public sealed class StringEqualsMetricTests
{
    private readonly StringEqualsMetric _metric = new();

    [Fact]
    public void CaseInsensitive_Match_Returns1()
    {
        _metric.GetSimilarity("hello", "HELLO").Should().Be(1.0f);
    }

    [Fact]
    public void Different_Strings_Returns0()
    {
        _metric.GetSimilarity("hello", "world").Should().Be(0.0f);
    }

    [Fact]
    public void Null_Input_Returns0()
    {
        _metric.GetSimilarity(null, "hello").Should().Be(0.0f);
        _metric.GetSimilarity("hello", null).Should().Be(0.0f);
    }

    [Fact]
    public void Name_ReturnsStringEquals()
    {
        _metric.Name.Should().Be("StringEquals");
    }
}

public sealed class SubstringMetricTests
{
    private readonly SubstringMetric _metric = new();

    [Fact]
    public void ContainsSubstring_Returns1()
    {
        _metric.GetSimilarity("Game of Thrones", "Game").Should().Be(1.0f);
    }

    [Fact]
    public void ReverseContains_Returns1()
    {
        _metric.GetSimilarity("Game", "Game of Thrones").Should().Be(1.0f);
    }

    [Fact]
    public void NoOverlap_Returns0()
    {
        _metric.GetSimilarity("abc", "xyz").Should().Be(0.0f);
    }

    [Fact]
    public void Empty_Input_Returns0()
    {
        _metric.GetSimilarity("", "hello").Should().Be(0.0f);
        _metric.GetSimilarity("hello", "").Should().Be(0.0f);
    }
}

public sealed class NameSimilarityMetricTests
{
    private readonly NameSimilarityMetric _metric = new();

    [Fact]
    public void ExactMatch_Returns1()
    {
        _metric.GetSimilarity("Game of Thrones", "Game of Thrones").Should().Be(1.0f);
    }

    [Fact]
    public void SimilarNames_ReturnsHighScore()
    {
        _metric.GetSimilarity("Game of Thrones", "game thrones").Should().BeGreaterThan(0.7f);
    }

    [Fact]
    public void CompletelyDifferent_ReturnsLowScore()
    {
        _metric.GetSimilarity("abc", "xyz").Should().BeLessThan(0.3f);
    }

    [Fact]
    public void Null_Input_Returns0()
    {
        _metric.GetSimilarity(null, "hello").Should().Be(0.0f);
    }

    [Fact]
    public void Empty_Input_Returns0()
    {
        _metric.GetSimilarity("", "hello").Should().Be(0.0f);
    }
}

public sealed class SeasonEpisodeMetricTests
{
    private readonly SeasonEpisodeMetric _metric = new();

    [Fact]
    public void ExactMatch_Returns1()
    {
        var ep1 = new Episode("Show", 1, 2, "Title");
        var ep2 = new Episode("Show", 1, 2, "Title");

        _metric.GetSimilarity(ep1, ep2).Should().Be(1.0f);
    }

    [Fact]
    public void SameSeasonDifferentEpisode_Returns05()
    {
        var ep1 = new Episode("Show", 1, 2, "Title A");
        var ep2 = new Episode("Show", 1, 3, "Title B");

        _metric.GetSimilarity(ep1, ep2).Should().Be(0.5f);
    }

    [Fact]
    public void DifferentSeasonDifferentEpisode_Returns0()
    {
        var ep1 = new Episode("Show", 1, 2, "Title A");
        var ep2 = new Episode("Show", 2, 5, "Title B");

        _metric.GetSimilarity(ep1, ep2).Should().Be(0.0f);
    }

    [Fact]
    public void StringInput_SxxExx_ParsedCorrectly()
    {
        _metric.GetSimilarity("S01E02", "S01E02").Should().Be(1.0f);
    }

    [Fact]
    public void NullInput_Returns0()
    {
        _metric.GetSimilarity(null, new Episode("Show", 1, 1, "T")).Should().Be(0.0f);
    }
}

public sealed class DateMetricTests
{
    private readonly DateMetric _metric = new();

    [Fact]
    public void SameDate_Returns1()
    {
        var d = new DateOnly(2023, 6, 15);
        _metric.GetSimilarity(d, d).Should().Be(1.0f);
    }

    [Fact]
    public void OneDayApart_ReturnsNear1()
    {
        var d1 = new DateOnly(2023, 6, 15);
        var d2 = new DateOnly(2023, 6, 16);

        _metric.GetSimilarity(d1, d2).Should().BeGreaterThan(0.95f);
    }

    [Fact]
    public void ThirtyDaysApart_ReturnsMuchLower()
    {
        var d1 = new DateOnly(2023, 6, 1);
        var d2 = new DateOnly(2023, 7, 1);

        var score = _metric.GetSimilarity(d1, d2);
        score.Should().BeLessThan(0.6f);
    }

    [Fact]
    public void SimpleDate_SupportedAsInput()
    {
        var sd1 = new SimpleDate(2023, 1, 1);
        var sd2 = new SimpleDate(2023, 1, 1);

        _metric.GetSimilarity(sd1, sd2).Should().Be(1.0f);
    }

    [Fact]
    public void Null_Returns0()
    {
        _metric.GetSimilarity(null, new DateOnly(2023, 1, 1)).Should().Be(0.0f);
    }
}

public sealed class MetricCascadeTests
{
    [Fact]
    public void ReturnsFirstNonZero()
    {
        var m1 = new StringEqualsMetric();
        var m2 = new SubstringMetric();
        var cascade = new MetricCascade(new ISimilarityMetric[] { m1, m2 });

        // "Game" != "Game of Thrones" by StringEquals (0), but SubstringMetric returns 1
        cascade.GetSimilarity("Game", "Game of Thrones").Should().Be(1.0f);
    }

    [Fact]
    public void AllZero_ReturnsZero()
    {
        var cascade = new MetricCascade(new ISimilarityMetric[] { new StringEqualsMetric() });
        cascade.GetSimilarity("abc", "xyz").Should().Be(0.0f);
    }
}

public sealed class MetricAvgTests
{
    [Fact]
    public void Averages_AllScores()
    {
        var m1 = new StringEqualsMetric();
        var m2 = new SubstringMetric();
        var avg = new MetricAvg(new ISimilarityMetric[] { m1, m2 });

        // "hello" == "hello" → StringEquals=1, Substring=1, avg=1
        avg.GetSimilarity("hello", "hello").Should().Be(1.0f);
    }

    [Fact]
    public void Empty_Metrics_ReturnsZero()
    {
        var avg = new MetricAvg(Array.Empty<ISimilarityMetric>());
        avg.GetSimilarity("a", "b").Should().Be(0.0f);
    }
}

public sealed class MetricMinTests
{
    [Fact]
    public void ReturnsMinimumScore()
    {
        var m1 = new StringEqualsMetric();
        var m2 = new SubstringMetric();
        var min = new MetricMin(new ISimilarityMetric[] { m1, m2 });

        // "Game" vs "Game of Thrones": StringEquals=0, Substring=1 → min=0
        min.GetSimilarity("Game", "Game of Thrones").Should().Be(0.0f);
    }

    [Fact]
    public void AllMatch_ReturnsOne()
    {
        var m1 = new StringEqualsMetric();
        var m2 = new SubstringMetric();
        var min = new MetricMin(new ISimilarityMetric[] { m1, m2 });

        min.GetSimilarity("hello", "hello").Should().Be(1.0f);
    }

    [Fact]
    public void Empty_Metrics_ReturnsZero()
    {
        var min = new MetricMin(Array.Empty<ISimilarityMetric>());
        min.GetSimilarity("a", "b").Should().Be(0.0f);
    }
}
