using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

public sealed class FileSizeMetric : ISimilarityMetric
{
    private const double Tolerance = 0.05;

    public string Name => "FileSize";

    public float GetSimilarity(object? a, object? b)
    {
        if (!TryGetSize(a, out var sizeA) || !TryGetSize(b, out var sizeB))
            return 0.0f;

        if (sizeA == 0 && sizeB == 0)
            return 1.0f;

        var max = Math.Max(sizeA, sizeB);
        if (max == 0)
            return 0.0f;

        var ratio = (double)Math.Abs(sizeA - sizeB) / max;

        if (ratio <= Tolerance)
            return 1.0f;

        // Scale down: 1 / (1 + excess ratio beyond tolerance)
        return (float)(1.0 / (1.0 + (ratio - Tolerance) * 10.0));
    }

    private static bool TryGetSize(object? value, out long size)
    {
        size = 0;
        if (value is null) return false;
        if (value is long l) { size = l; return true; }
        if (value is int i) { size = i; return true; }
        if (value is double d) { size = (long)d; return true; }
        if (value is FileInfo fi) { size = fi.Length; return true; }

        return long.TryParse(value.ToString(), out size);
    }
}
