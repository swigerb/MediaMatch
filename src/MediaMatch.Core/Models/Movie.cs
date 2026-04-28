namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a movie search result with basic identifying information.
/// </summary>
/// <param name="Name">The title of the movie.</param>
/// <param name="Year">The release year.</param>
/// <param name="TmdbId">The TMDb identifier, or <c>null</c> if unavailable.</param>
/// <param name="ImdbId">The IMDb identifier, or <c>null</c> if unavailable.</param>
/// <param name="Language">The original language code, or <c>null</c> if unknown.</param>
public sealed record Movie(
    string Name,
    int Year,
    int? TmdbId = null,
    int? ImdbId = null,
    string? Language = null);
