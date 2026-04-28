namespace MediaMatch.Application.Detection;

/// <summary>
/// Represents a parsed season and episode match from a media filename.
/// </summary>
/// <param name="Season">Gets the season number.</param>
/// <param name="Episode">Gets the starting episode number.</param>
/// <param name="EndEpisode">Gets the ending episode number for multi-episode ranges, if applicable.</param>
/// <param name="AbsoluteNumber">Gets the absolute episode number, if detected.</param>
/// <param name="IsSpecial">Gets a value indicating whether this is a special episode.</param>
public sealed record SeasonEpisodeMatch(
    int Season,
    int Episode,
    int? EndEpisode = null,
    int? AbsoluteNumber = null,
    bool IsSpecial = false)
{
    /// <summary>True when the match represents multiple episodes (e.g. S01E01-E03).</summary>
    public bool IsMultiEpisode => EndEpisode.HasValue && EndEpisode.Value != Episode;
}
