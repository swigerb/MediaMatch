using MediaMatch.Application.Matching.Metrics;
using MediaMatch.Core.Matching;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Matching;

/// <summary>
/// Matches file paths to episodes using season/episode numbers, name similarity, and date metrics.
/// </summary>
public sealed class EpisodeMatcher
{
    /// <summary>
    /// Matches file paths to episodes using a cascading set of similarity metrics.
    /// </summary>
    /// <param name="filePaths">The file paths to match.</param>
    /// <param name="episodes">The candidate episodes to match against.</param>
    /// <returns>A list of file-to-episode matches ordered by descending score.</returns>
    public IReadOnlyList<Match<string, Episode>> MatchFiles(
        IReadOnlyList<string> filePaths,
        IReadOnlyList<Episode> episodes)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        ArgumentNullException.ThrowIfNull(episodes);

        var metrics = new ISimilarityMetric[]
        {
            new MetricCascade(
            [
                new SeasonEpisodeMetric(),
                new NameSimilarityMetric(),
                new DateMetric(),
            ]),
        };

        var matcher = new BipartiteMatcher<string, Episode>(metrics, threshold: 0.4f);

        return matcher.Match(
            filePaths,
            episodes,
            filePath => filePath,
            episode => episode);
    }
}
