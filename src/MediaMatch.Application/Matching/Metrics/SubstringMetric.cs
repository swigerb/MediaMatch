using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

/// <summary>
/// Computes similarity by checking whether either string value contains the other as a substring (case-insensitive).
/// </summary>
public sealed class SubstringMetric : ISimilarityMetric
{
    /// <inheritdoc/>
    public string Name => "Substring";

    /// <inheritdoc/>
    public float GetSimilarity(object? a, object? b)
    {
        var sa = a?.ToString();
        var sb = b?.ToString();

        if (string.IsNullOrEmpty(sa) || string.IsNullOrEmpty(sb))
            return 0.0f;

        if (sa.Contains(sb, StringComparison.OrdinalIgnoreCase) ||
            sb.Contains(sa, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0f;
        }

        return 0.0f;
    }
}
