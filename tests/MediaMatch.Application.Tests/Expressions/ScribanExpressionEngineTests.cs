using FluentAssertions;
using MediaMatch.Application.Expressions;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Tests.Expressions;

public sealed class ScribanExpressionEngineTests
{
    private readonly ScribanExpressionEngine _engine = new();

    // ── Basic variable substitution ─────────────────────────────────────

    [Fact]
    public void Evaluate_BasicVariable_Substituted()
    {
        var episode = new Episode("Breaking Bad", 1, 1, "Pilot");
        var bindings = MediaBindings.ForEpisode(episode);

        var result = _engine.Evaluate("{{n}}", bindings);

        result.Should().Be("Breaking Bad");
    }

    [Fact]
    public void Evaluate_SeasonEpisodeFormat()
    {
        var episode = new Episode("Show", 1, 2, "Title");
        var bindings = MediaBindings.ForEpisode(episode);

        var result = _engine.Evaluate("{{s00e00}}", bindings);

        result.Should().Be("S01E02");
    }

    // ── FileBot syntax conversion ───────────────────────────────────────

    [Fact]
    public void Evaluate_FileBotSingleBrace_ConvertedAndEvaluated()
    {
        var episode = new Episode("Breaking Bad", 1, 1, "Pilot");
        var bindings = MediaBindings.ForEpisode(episode);

        var result = _engine.Evaluate("{n}", bindings);

        result.Should().Be("Breaking Bad");
    }

    [Fact]
    public void ConvertFromFileBotSyntax_ConvertsCorrectly()
    {
        var converted = ScribanExpressionEngine.ConvertFromFileBotSyntax("{n} ({y})");

        converted.Should().Be("{{n}} ({{y}})");
    }

    [Fact]
    public void ConvertFromFileBotSyntax_LeavesDoubleBracesAlone()
    {
        var converted = ScribanExpressionEngine.ConvertFromFileBotSyntax("{{n}} ({{y}})");

        converted.Should().Be("{{n}} ({{y}})");
    }

    // ── Movie bindings ──────────────────────────────────────────────────

    [Fact]
    public void Evaluate_MovieBindings()
    {
        var movie = new Movie("Inception", 2010);
        var bindings = MediaBindings.ForMovie(movie);

        var result = _engine.Evaluate("{{n}} ({{y}})", bindings);

        result.Should().Be("Inception (2010)");
    }

    // ── Built-in Scriban functions ───────────────────────────────────────

    [Fact]
    public void Evaluate_StringUpcase()
    {
        var episode = new Episode("Breaking Bad", 1, 1, "Pilot");
        var bindings = MediaBindings.ForEpisode(episode);

        var result = _engine.Evaluate("{{n | string.upcase}}", bindings);

        result.Should().Be("BREAKING BAD");
    }

    // ── Custom helpers (mm.*) ───────────────────────────────────────────

    [Fact]
    public void Evaluate_MmPad()
    {
        var episode = new Episode("Show", 1, 2, "Title");
        var bindings = MediaBindings.ForEpisode(episode);

        var result = _engine.Evaluate("{{mm.pad e 2}}", bindings);

        result.Should().Be("02");
    }

    [Fact]
    public void Evaluate_MmCleanFilename_StripsInvalidChars()
    {
        var episode = new Episode("Show", 1, 1, "Title: With/Bad\\Chars");
        var bindings = MediaBindings.ForEpisode(episode);

        var result = _engine.Evaluate("{{mm.clean_filename t}}", bindings);

        result.Should().NotContain(":");
        result.Should().NotContain("/");
        result.Should().NotContain("\\");
    }

    // ── Missing bindings ────────────────────────────────────────────────

    [Fact]
    public void Evaluate_MissingVariable_ReturnsEmptyNotError()
    {
        var episode = new Episode("Show", 1, 1, "Title");
        var bindings = MediaBindings.ForEpisode(episode);

        var result = _engine.Evaluate("{{missing_var}}", bindings);

        // Should not throw, returns empty or renders without crash
        result.Should().NotBeNull();
    }

    // ── Validation ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidExpression_ReturnsTrue()
    {
        var isValid = _engine.Validate("{{n}} - {{s00e00}}", out var error);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Validate_InvalidExpression_ReturnsFalseWithError()
    {
        // Scriban treats "{{" without closing "}}" as an opening code block error
        // Use a clearly malformed Scriban expression
        var isValid = _engine.Validate("{{ if }}", out var error);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    // ── Complex real-world expressions ──────────────────────────────────

    [Fact]
    public void Evaluate_FullPathExpression_FileBotSyntax()
    {
        var episode = new Episode("Breaking Bad", 1, 1, "Pilot");
        var series = new SeriesInfo(
            "Breaking Bad", "1", "Overview", "AMC", "Ended",
            9.5, 47, new[] { "Drama" },
            StartDate: new SimpleDate(2008, 1, 20));
        var bindings = MediaBindings.ForEpisode(episode, series);

        var result = _engine.Evaluate("{n}/Season {s00}/{n} - {s00e00} - {t}", bindings);

        result.Should().Contain("Breaking Bad");
        result.Should().Contain("Season 01");
        result.Should().Contain("S01E01");
        result.Should().Contain("Pilot");
    }

    [Fact]
    public void Evaluate_MoviePath_FileBotSyntax()
    {
        var movie = new Movie("Inception", 2010);
        var bindings = MediaBindings.ForMovie(movie);

        var result = _engine.Evaluate("{n} ({y})", bindings);

        result.Should().Be("Inception (2010)");
    }

    // ── Episode additional bindings ─────────────────────────────────────

    [Fact]
    public void Evaluate_EpisodePadded_s00e00()
    {
        var episode = new Episode("Show", 3, 14, "Title");
        var bindings = MediaBindings.ForEpisode(episode);

        _engine.Evaluate("{{s00}}", bindings).Should().Be("03");
        _engine.Evaluate("{{e00}}", bindings).Should().Be("14");
        _engine.Evaluate("{{s00e00}}", bindings).Should().Be("S03E14");
    }

    [Fact]
    public void Evaluate_SxEFormat()
    {
        var episode = new Episode("Show", 2, 5, "Title");
        var bindings = MediaBindings.ForEpisode(episode);

        _engine.Evaluate("{{sxe}}", bindings).Should().Be("2x05");
    }
}
