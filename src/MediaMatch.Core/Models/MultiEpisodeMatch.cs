namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a multi-episode match with episode range and merged metadata.
/// </summary>
public sealed record MultiEpisodeMatch(
    string SeriesName,
    int Season,
    int StartEpisode,
    int EndEpisode,
    IReadOnlyList<int> EpisodeNumbers,
    string MergedTitle)
{
    /// <summary>Number of episodes in the range.</summary>
    public int EpisodeCount => EpisodeNumbers.Count;

    /// <summary>Creates from a list of episodes.</summary>
    public static MultiEpisodeMatch FromEpisodes(IReadOnlyList<Episode> episodes)
    {
        if (episodes.Count == 0)
            throw new ArgumentException("At least one episode is required.", nameof(episodes));

        var first = episodes[0];
        var numbers = episodes.Select(e => e.EpisodeNumber).OrderBy(n => n).ToList();
        var titles = episodes
            .Where(e => !string.IsNullOrWhiteSpace(e.Title))
            .Select(e => e.Title);
        var mergedTitle = string.Join(" & ", titles);

        return new MultiEpisodeMatch(
            SeriesName: first.SeriesName,
            Season: first.Season,
            StartEpisode: numbers[0],
            EndEpisode: numbers[^1],
            EpisodeNumbers: numbers,
            MergedTitle: mergedTitle);
    }
}
