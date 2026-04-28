using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

/// <summary>
/// Returns the minimum score across all metrics (strict AND).
/// </summary>
public sealed class MetricMin : ISimilarityMetric
{
    private readonly IReadOnlyList<ISimilarityMetric> _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricMin"/> class.
    /// </summary>
    /// <param name="metrics">The collection of metrics whose minimum score is returned.</param>
    public MetricMin(IReadOnlyList<ISimilarityMetric> metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        _metrics = metrics;
    }

    /// <inheritdoc/>
    public string Name => "Min";

    /// <inheritdoc/>
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
