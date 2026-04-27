namespace MediaMatch.Core.Matching;

public interface ISimilarityMetric
{
    string Name { get; }

    float GetSimilarity(object? a, object? b);
}
