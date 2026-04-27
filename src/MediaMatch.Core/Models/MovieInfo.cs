namespace MediaMatch.Core.Models;

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
