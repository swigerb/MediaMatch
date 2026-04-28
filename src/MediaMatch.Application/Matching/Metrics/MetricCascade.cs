using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

/// <summary>
/// Tries metrics in sequence, returns the first non-zero result.
/// </summary>
public sealed class MetricCascade : ISimilarityMetric
{
    private readonly IReadOnlyList<ISimilarityMetric> _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricCascade"/> class.
    /// </summary>
    /// <param name="metrics">The ordered collection of metrics to try in sequence.</param>
    public MetricCascade(IReadOnlyList<ISimilarityMetric> metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        _metrics = metrics;
    }

    /// <inheritdoc/>
    public string Name => "Cascade";

    /// <inheritdoc/>
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
