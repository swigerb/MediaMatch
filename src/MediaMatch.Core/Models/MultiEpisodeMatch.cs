namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a multi-episode match with episode range and merged metadata.
/// </summary>
/// <param name="SeriesName">The name of the parent series.</param>
/// <param name="Season">The season number.</param>
/// <param name="StartEpisode">The first episode number in the range.</param>
/// <param name="EndEpisode">The last episode number in the range.</param>
/// <param name="EpisodeNumbers">The ordered list of episode numbers in the range.</param>
/// <param name="MergedTitle">The combined title string from all episodes.</param>
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

    /// <summary>Creates a <see cref="MultiEpisodeMatch"/> from a list of episodes.</summary>
    /// <param name="episodes">The episodes to merge. Must contain at least one episode.</param>
    /// <returns>A <see cref="MultiEpisodeMatch"/> spanning the provided episodes.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="episodes"/> is empty.</exception>
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
