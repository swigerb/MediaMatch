namespace MediaMatch.Core.Models;

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
