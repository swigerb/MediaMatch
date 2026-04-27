namespace MediaMatch.Core.Models;

public sealed record SeriesInfo(
    string Name,
    string? Id,
    string? Overview,
    string? Network,
    string? Status,
    double? Rating,
    int? Runtime,
    IReadOnlyList<string> Genres,
    string? PosterUrl = null,
    string? BannerUrl = null,
    SimpleDate? StartDate = null,
    string? ImdbId = null,
    int? TmdbId = null,
    string? Language = null,
    IReadOnlyList<string>? AliasNames = null);
