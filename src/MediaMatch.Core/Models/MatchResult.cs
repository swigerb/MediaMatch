using MediaMatch.Core.Enums;

namespace MediaMatch.Core.Models;

/// <summary>
/// Represents the result of matching a file against metadata providers.
/// Contains detected media info, confidence, and provider attribution.
/// </summary>
/// <param name="MediaType">The detected media type.</param>
/// <param name="Confidence">The match confidence score between 0 and 1.</param>
/// <param name="ProviderSource">The name of the provider that produced this match.</param>
/// <param name="Episode">The matched episode metadata, if applicable.</param>
/// <param name="Movie">The matched movie metadata, if applicable.</param>
/// <param name="SeriesInfo">The matched series metadata, if applicable.</param>
/// <param name="MovieInfo">The matched detailed movie metadata, if applicable.</param>
public sealed record MatchResult(
    MediaType MediaType,
    float Confidence,
    string ProviderSource,
    Episode? Episode = null,
    Movie? Movie = null,
    SeriesInfo? SeriesInfo = null,
    MovieInfo? MovieInfo = null)
{
    /// <summary>Gets a value indicating whether a match was found.</summary>
    public bool IsMatch => Confidence > 0f;

    /// <summary>Creates a <see cref="MatchResult"/> representing no match for the specified media type.</summary>
    /// <param name="mediaType">The media type that failed to match.</param>
    /// <returns>A <see cref="MatchResult"/> with zero confidence.</returns>
    public static MatchResult NoMatch(MediaType mediaType) =>
        new(mediaType, 0f, "none");
}
