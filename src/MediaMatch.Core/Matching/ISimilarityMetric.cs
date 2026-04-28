namespace MediaMatch.Core.Matching;

/// <summary>
/// Computes the similarity between two objects using a named metric.
/// </summary>
public interface ISimilarityMetric
{
    /// <summary>Gets the display name of this similarity metric.</summary>
    string Name { get; }

    /// <summary>
    /// Computes the similarity between two objects.
    /// </summary>
    /// <param name="a">The first object to compare.</param>
    /// <param name="b">The second object to compare.</param>
    /// <returns>A similarity score from 0.0 (no match) to 1.0 (exact match).</returns>
    float GetSimilarity(object? a, object? b);
}
