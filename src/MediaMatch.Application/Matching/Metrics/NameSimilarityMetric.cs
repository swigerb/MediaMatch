using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

/// <summary>
/// Computes similarity between two names using normalized longest common subsequence scoring.
/// </summary>
public sealed class NameSimilarityMetric : ISimilarityMetric
{
    /// <inheritdoc/>
    public string Name => "NameSimilarity";

    /// <inheritdoc/>
    public float GetSimilarity(object? a, object? b)
    {
        var sa = a?.ToString();
        var sb = b?.ToString();

        if (string.IsNullOrEmpty(sa) || string.IsNullOrEmpty(sb))
            return 0.0f;

        var na = Normalization.NormalizeName(sa);
        var nb = Normalization.NormalizeName(sb);

        if (na.Length == 0 || nb.Length == 0)
            return 0.0f;

        if (string.Equals(na, nb, StringComparison.Ordinal))
            return 1.0f;

        var lcsLen = LongestCommonSubsequenceLength(na, nb);
        return 2.0f * lcsLen / (na.Length + nb.Length);
    }

    private static int LongestCommonSubsequenceLength(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        // Space-optimized LCS using two rows
        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                curr[j] = a[i - 1] == b[j - 1]
                    ? prev[j - 1] + 1
                    : Math.Max(prev[j], curr[j - 1]);
            }

            (prev, curr) = (curr, prev);
            Array.Clear(curr);
        }

        return prev[b.Length];
    }
}
