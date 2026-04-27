using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

/// <summary>
/// Tries metrics in sequence, returns the first non-zero result.
/// </summary>
public sealed class MetricCascade : ISimilarityMetric
{
    private readonly IReadOnlyList<ISimilarityMetric> _metrics;

    public MetricCascade(IReadOnlyList<ISimilarityMetric> metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        _metrics = metrics;
    }

    public string Name => "Cascade";

    public float GetSimilarity(object? a, object? b)
    {
        foreach (var metric in _metrics)
        {
            var score = metric.GetSimilarity(a, b);
            if (score > 0.0f)
                return score;
        }

        return 0.0f;
    }
}
