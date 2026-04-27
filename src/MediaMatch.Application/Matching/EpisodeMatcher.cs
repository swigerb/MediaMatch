using MediaMatch.Application.Matching.Metrics;
using MediaMatch.Core.Matching;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Matching;

public class EpisodeMatcher
{
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
