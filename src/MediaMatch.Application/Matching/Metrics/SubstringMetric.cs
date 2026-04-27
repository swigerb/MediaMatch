using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

public sealed class SubstringMetric : ISimilarityMetric
{
    public string Name => "Substring";

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
