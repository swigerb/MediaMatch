using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching.Metrics;

public sealed class FileNameMetric : ISimilarityMetric
{
    private readonly NameSimilarityMetric _nameSimilarity = new();

    public string Name => "FileName";

    public float GetSimilarity(object? a, object? b)
    {
        var sa = a?.ToString();
        var sb = b?.ToString();

        if (string.IsNullOrEmpty(sa) || string.IsNullOrEmpty(sb))
            return 0.0f;

        var na = Normalization.NormalizeName(
            Normalization.StripReleaseInfo(
                Normalization.StripExtension(sa)));

        var nb = Normalization.NormalizeName(
            Normalization.StripReleaseInfo(
                Normalization.StripExtension(sb)));

        return _nameSimilarity.GetSimilarity(na, nb);
    }
}
