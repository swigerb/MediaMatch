using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

/// <summary>
/// Computes similarity by performing a case-insensitive string equality check, returning 1.0 or 0.0.
/// </summary>
public sealed class StringEqualsMetric : ISimilarityMetric
{
    /// <inheritdoc/>
    public string Name => "StringEquals";

    /// <inheritdoc/>
    public float GetSimilarity(object? a, object? b)
    {
        var sa = a?.ToString();
        var sb = b?.ToString();

        if (sa is null || sb is null)
            return 0.0f;

        return string.Equals(sa, sb, StringComparison.OrdinalIgnoreCase) ? 1.0f : 0.0f;
    }
}
