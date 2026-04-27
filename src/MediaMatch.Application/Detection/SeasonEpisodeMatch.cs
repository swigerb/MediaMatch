namespace MediaMatch.Application.Detection;

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
