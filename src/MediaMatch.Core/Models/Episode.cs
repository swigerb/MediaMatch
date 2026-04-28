namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a single TV series or anime episode with identifying metadata.
/// </summary>
/// <param name="SeriesName">The name of the parent series.</param>
/// <param name="Season">The season number.</param>
/// <param name="EpisodeNumber">The episode number within the season.</param>
/// <param name="Title">The episode title.</param>
/// <param name="AbsoluteNumber">The absolute episode number across all seasons, or <c>null</c> if unavailable.</param>
/// <param name="Special">The special episode number, or <c>null</c> if this is a regular episode.</param>
/// <param name="AirDate">The original air date, or <c>null</c> if unknown.</param>
/// <param name="SeriesId">The provider-specific series identifier.</param>
/// <param name="SortOrder">The episode ordering scheme used by the provider.</param>
public sealed record Episode(
    string SeriesName,
    int Season,
    int EpisodeNumber,
    string Title,
    int? AbsoluteNumber = null,
    int? Special = null,
    SimpleDate? AirDate = null,
    string? SeriesId = null,
    SortOrder SortOrder = SortOrder.Airdate);
