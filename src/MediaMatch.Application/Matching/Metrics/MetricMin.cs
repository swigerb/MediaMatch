using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

/// <summary>
/// Returns the minimum score across all metrics (strict AND).
/// </summary>
public sealed class MetricMin : ISimilarityMetric
{
    private readonly IReadOnlyList<ISimilarityMetric> _metrics;

    public MetricMin(IReadOnlyList<ISimilarityMetric> metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        _metrics = metrics;
    }

    public string Name => "Min";

    public float GetSimilarity(object? a, object? b)
    {
        if (_metrics.Count == 0)
            return 0.0f;

        var min = float.MaxValue;
        foreach (var metric in _metrics)
        {
            var score = metric.GetSimilarity(a, b);
            if (score < min)
                min = score;
        }

        return min;
    }
}
