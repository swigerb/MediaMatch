using System.Globalization;
using System.Text.RegularExpressions;
using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

public sealed partial class NumericSimilarityMetric : ISimilarityMetric
{
    [GeneratedRegex(@"-?\d+(\.\d+)?", RegexOptions.Compiled)]
    private static partial Regex NumberPattern();

    public string Name => "NumericSimilarity";

    public float GetSimilarity(object? a, object? b)
    {
        if (!TryExtractNumber(a, out var na) || !TryExtractNumber(b, out var nb))
            return 0.0f;

        if (Math.Abs(na - nb) < 0.0001)
            return 1.0f;

        // Scale by closeness: 1 / (1 + relative difference)
        var max = Math.Max(Math.Abs(na), Math.Abs(nb));
        if (max < 0.0001)
            return 1.0f;

        var relativeDiff = Math.Abs(na - nb) / max;
        return (float)(1.0 / (1.0 + relativeDiff));
    }

    private static bool TryExtractNumber(object? value, out double result)
    {
        result = 0;

        if (value is null)
            return false;

        if (value is int i) { result = i; return true; }
        if (value is long l) { result = l; return true; }
        if (value is float f) { result = f; return true; }
        if (value is double d) { result = d; return true; }
        if (value is decimal dec) { result = (double)dec; return true; }

        var text = value.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (double.TryParse(text, CultureInfo.InvariantCulture, out result))
            return true;

        var match = NumberPattern().Match(text);
        if (match.Success && double.TryParse(match.Value, CultureInfo.InvariantCulture, out result))
            return true;

        return false;
    }
}
