namespace MediaMatch.Core.Models;

/// <summary>
/// Represents detailed TV series metadata retrieved from a metadata provider.
/// </summary>
/// <param name="Name">The series title.</param>
/// <param name="Id">The provider-specific series identifier.</param>
/// <param name="Overview">The series synopsis.</param>
/// <param name="Network">The original broadcast network or streaming service.</param>
/// <param name="Status">The airing status (e.g., "Continuing", "Ended").</param>
/// <param name="Rating">The community rating score.</param>
/// <param name="Runtime">The typical episode runtime in minutes.</param>
/// <param name="Genres">The list of genre names.</param>
/// <param name="PosterUrl">The URL of the poster image.</param>
/// <param name="BannerUrl">The URL of the banner image.</param>
/// <param name="StartDate">The premiere date of the series.</param>
/// <param name="ImdbId">The IMDb identifier.</param>
/// <param name="TmdbId">The TMDb identifier.</param>
/// <param name="Language">The original language code.</param>
/// <param name="AliasNames">Alternative titles or aliases for the series.</param>
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
