using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

/// <summary>
/// Returns the average score across all metrics.
/// </summary>
public sealed class MetricAvg : ISimilarityMetric
{
    private readonly IReadOnlyList<ISimilarityMetric> _metrics;

    public MetricAvg(IReadOnlyList<ISimilarityMetric> metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        _metrics = metrics;
    }

    public string Name => "Average";

    public float GetSimilarity(object? a, object? b)
    {
        if (_metrics.Count == 0)
            return 0.0f;

        var sum = 0.0f;
        foreach (var metric in _metrics)
        {
            sum += metric.GetSimilarity(a, b);
        }

        return sum / _metrics.Count;
    }
}
