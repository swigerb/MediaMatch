using FluentAssertions;
using MediaMatch.Application.Detection;

namespace MediaMatch.Application.Tests.Detection;

public sealed class MultiEpisodePatternTests
{
    private readonly ReleaseInfoParser _parser = new();

    // ── S01E01-E02 Pattern ──────────────────────────────────────
    [Fact]
    public void Parse_SxxExxDashExx_DetectsMultiEpisode()
    {
        var info = _parser.Parse("Show.S01E01-E03.1080p.mkv");
        info.SeasonEpisode.Should().NotBeNull();
        info.SeasonEpisode!.Season.Should().Be(1);
        info.SeasonEpisode.Episode.Should().Be(1);
        info.SeasonEpisode.EndEpisode.Should().Be(3);
        info.SeasonEpisode.IsMultiEpisode.Should().BeTrue();
    }

    // ── S01E01E02 Pattern (no dash) ─────────────────────────────
    [Fact]
    public void Parse_SxxExxExx_DetectsMultiEpisode()
    {
        var info = _parser.Parse("Show.S02E05E06.720p.mkv");
        info.SeasonEpisode.Should().NotBeNull();
        info.SeasonEpisode!.Season.Should().Be(2);
        info.SeasonEpisode.Episode.Should().Be(5);
        info.SeasonEpisode.EndEpisode.Should().Be(6);
    }

    // ── S01E01-S01E02 Pattern ───────────────────────────────────
    [Fact]
    public void Parse_SxxExxDashSxxExx_DetectsMultiEpisode()
    {
        var info = _parser.Parse("Show.S01E01-S01E02.1080p.mkv");
        info.SeasonEpisode.Should().NotBeNull();
        info.SeasonEpisode!.Season.Should().Be(1);
        info.SeasonEpisode.Episode.Should().Be(1);
        info.SeasonEpisode.EndEpisode.Should().Be(2);
    }

    // ── 1x01-1x02 Pattern ──────────────────────────────────────
    [Fact]
    public void Parse_NxNNRangePattern_DetectsMultiEpisode()
    {
        var info = _parser.Parse("Show.1x01-1x03.mkv");
        info.SeasonEpisode.Should().NotBeNull();
        info.SeasonEpisode!.Season.Should().Be(1);
        info.SeasonEpisode.Episode.Should().Be(1);
        info.SeasonEpisode.EndEpisode.Should().Be(3);
    }

    // ── Episode 1-2 Word Pattern ────────────────────────────────
    [Fact]
    public void Parse_EpisodeRangeWord_DetectsMultiEpisode()
    {
        var info = _parser.Parse("Show Episode 5-7.mkv");
        info.SeasonEpisode.Should().NotBeNull();
        info.SeasonEpisode!.Episode.Should().Be(5);
        info.SeasonEpisode.EndEpisode.Should().Be(7);
    }

    // ── Absolute range: 01-02 ───────────────────────────────────
    [Fact]
    public void Parse_AbsoluteRange_DetectsMultiEpisode()
    {
        var info = _parser.Parse("[Sub] Anime Title 01-03.mkv");
        info.SeasonEpisode.Should().NotBeNull();
        info.SeasonEpisode!.Episode.Should().Be(1);
        info.SeasonEpisode.EndEpisode.Should().Be(3);
    }

    // ── Single episode ──────────────────────────────────────────
    [Fact]
    public void Parse_SingleEpisode_NoEndEpisode()
    {
        var info = _parser.Parse("Show.S01E05.1080p.mkv");
        info.SeasonEpisode.Should().NotBeNull();
        info.SeasonEpisode!.Episode.Should().Be(5);
        info.SeasonEpisode!.EndEpisode.Should().BeNull();
        info.SeasonEpisode!.IsMultiEpisode.Should().BeFalse();
    }

    // ── SeasonEpisodeMatch ──────────────────────────────────────
    [Fact]
    public void SeasonEpisodeMatch_IsMultiEpisode_WhenEndDiffers()
    {
        var match = new SeasonEpisodeMatch(1, 1, EndEpisode: 3);
        match.IsMultiEpisode.Should().BeTrue();
    }

    [Fact]
    public void SeasonEpisodeMatch_NotMultiEpisode_WhenEndEquals()
    {
        var match = new SeasonEpisodeMatch(1, 1, EndEpisode: 1);
        match.IsMultiEpisode.Should().BeFalse();
    }

    [Fact]
    public void SeasonEpisodeMatch_NotMultiEpisode_WhenEndNull()
    {
        var match = new SeasonEpisodeMatch(1, 1);
        match.IsMultiEpisode.Should().BeFalse();
    }

    // ── Standard single patterns still work ──────────────────────
    [Theory]
    [InlineData("Show.S01E01.mkv", 1, 1)]
    [InlineData("Show.S03E25.mkv", 3, 25)]
    [InlineData("Show.1x01.mkv", 1, 1)]
    [InlineData("Show Season 2 Episode 5.mkv", 2, 5)]
    public void Parse_StandardPatterns_DetectedCorrectly(string filename, int season, int episode)
    {
        var info = _parser.Parse(filename);
        info.SeasonEpisode.Should().NotBeNull();
        info.SeasonEpisode!.Season.Should().Be(season);
        info.SeasonEpisode!.Episode.Should().Be(episode);
    }

    // ── Ep.01-02 pattern ────────────────────────────────────────
    [Fact]
    public void Parse_EpDotRange_DetectsMultiEpisode()
    {
        var info = _parser.Parse("Show Ep.01-02.mkv");
        info.SeasonEpisode.Should().NotBeNull();
        info.SeasonEpisode!.Episode.Should().Be(1);
        info.SeasonEpisode.EndEpisode.Should().Be(2);
    }

    // ── Quality still detected with multi-episode ────────────────
    [Fact]
    public void Parse_MultiEpisodeWithQuality_BothDetected()
    {
        var info = _parser.Parse("Show.S01E01-E02.1080p.BluRay.mkv");
        info.SeasonEpisode.Should().NotBeNull();
        info.SeasonEpisode!.IsMultiEpisode.Should().BeTrue();
        info.Quality.Should().Be(MediaMatch.Core.Enums.VideoQuality.HD1080p);
    }
}
