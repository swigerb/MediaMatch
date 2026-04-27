using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

public sealed class StringEqualsMetric : ISimilarityMetric
{
    public string Name => "StringEquals";

    public float GetSimilarity(object? a, object? b)
    {
        var sa = a?.ToString();
        var sb = b?.ToString();

        if (sa is null || sb is null)
            return 0.0f;

        return string.Equals(sa, sb, StringComparison.OrdinalIgnoreCase) ? 1.0f : 0.0f;
    }
}
