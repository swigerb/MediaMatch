using MediaMatch.Core.Enums;

namespace MediaMatch.Core.Models;

/// <summary>
/// Result of matching a file against metadata providers.
/// Contains detected media info, confidence, and provider attribution.
/// </summary>
public sealed record MatchResult(
    MediaType MediaType,
    float Confidence,
    string ProviderSource,
    Episode? Episode = null,
    Movie? Movie = null,
    SeriesInfo? SeriesInfo = null,
    MovieInfo? MovieInfo = null)
{
    public bool IsMatch => Confidence > 0f;

    public static MatchResult NoMatch(MediaType mediaType) =>
        new(mediaType, 0f, "none");
}
