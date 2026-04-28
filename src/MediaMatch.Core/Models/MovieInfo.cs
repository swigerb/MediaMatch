namespace MediaMatch.Core.Models;

/// <summary>
/// Represents detailed movie metadata retrieved from a metadata provider.
/// </summary>
/// <param name="Name">The title of the movie.</param>
/// <param name="Year">The release year.</param>
/// <param name="TmdbId">The TMDb identifier, or <c>null</c> if unavailable.</param>
/// <param name="ImdbId">The IMDb identifier, or <c>null</c> if unavailable.</param>
/// <param name="Overview">The plot synopsis.</param>
/// <param name="Tagline">The marketing tagline.</param>
/// <param name="PosterUrl">The URL of the poster image.</param>
/// <param name="Rating">The community rating score.</param>
/// <param name="Runtime">The runtime in minutes.</param>
/// <param name="Certification">The content rating certification (e.g., "PG-13").</param>
/// <param name="Genres">The list of genre names.</param>
/// <param name="Cast">The list of cast members.</param>
/// <param name="Crew">The list of crew members.</param>
/// <param name="OriginalLanguage">The original language code.</param>
/// <param name="OriginalTitle">The original title in the source language.</param>
/// <param name="Revenue">The total box-office revenue in USD.</param>
/// <param name="Budget">The production budget in USD.</param>
/// <param name="Collection">The name of the collection the movie belongs to.</param>
public sealed record MovieInfo(
    string Name,
    int Year,
    int? TmdbId,
    string? ImdbId,
    string? Overview,
    string? Tagline,
    string? PosterUrl,
    double? Rating,
    int? Runtime,
    string? Certification,
    IReadOnlyList<string> Genres,
    IReadOnlyList<Person> Cast,
    IReadOnlyList<Person> Crew,
    string? OriginalLanguage = null,
    string? OriginalTitle = null,
    long? Revenue = null,
    long? Budget = null,
    string? Collection = null);
