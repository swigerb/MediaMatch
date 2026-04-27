using System.Text.RegularExpressions;
using MediaMatch.Core.Matching;
using MediaMatch.Core.Models;

namespace MediaMatch.Application.Matching.Metrics;

public sealed partial class SeasonEpisodeMetric : ISimilarityMetric
{
    [GeneratedRegex(@"S(\d+)E(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SeasonEpisodePattern();

    public string Name => "SeasonEpisode";

    public float GetSimilarity(object? a, object? b)
    {
        if (!TryExtract(a, out var sa, out var ea) || !TryExtract(b, out var sb, out var eb))
            return 0.0f;

        var seasonMatch = sa == sb;
        var episodeMatch = ea == eb;

        if (seasonMatch && episodeMatch)
            return 1.0f;
        if (seasonMatch || episodeMatch)
            return 0.5f;

        return 0.0f;
    }

    private static bool TryExtract(object? value, out int season, out int episode)
    {
        season = 0;
        episode = 0;

        if (value is null)
            return false;

        if (value is Episode ep)
        {
            season = ep.Season;
            episode = ep.EpisodeNumber;
            return true;
        }

        // Try parsing SxxExx from string
        var text = value.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var match = SeasonEpisodePattern().Match(text);
        if (match.Success)
        {
            season = int.Parse(match.Groups[1].Value);
            episode = int.Parse(match.Groups[2].Value);
            return true;
        }

        return false;
    }
}
