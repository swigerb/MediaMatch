using MediaMatch.Core.Matching;

namespace MediaMatch.Application.Matching;

/// <summary>
/// Performs greedy bipartite matching between values and candidates using configurable similarity metrics.
/// </summary>
/// <typeparam name="TValue">The type of values to match.</typeparam>
/// <typeparam name="TCandidate">The type of candidates to match against.</typeparam>
public sealed class BipartiteMatcher<TValue, TCandidate>
{
    private readonly IReadOnlyList<ISimilarityMetric> _metrics;
    private readonly float _threshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="BipartiteMatcher{TValue, TCandidate}"/> class.
    /// </summary>
    /// <param name="metrics">The similarity metrics used to score value-candidate pairs.</param>
    /// <param name="threshold">The minimum score required for a pair to be considered a match.</param>
    public BipartiteMatcher(IReadOnlyList<ISimilarityMetric> metrics, float threshold = 0.5f)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        _metrics = metrics;
        _threshold = threshold;
    }

    /// <summary>
    /// Matches values to candidates using a greedy best-first strategy, returning disjoint one-to-one pairings.
    /// </summary>
    /// <param name="values">The values to match.</param>
    /// <param name="candidates">The candidates to match against.</param>
    /// <param name="valueTransform">Transforms a value into the object used for similarity comparison.</param>
    /// <param name="candidateTransform">Transforms a candidate into the object used for similarity comparison.</param>
    /// <returns>A list of disjoint matches sorted by descending score.</returns>
    public IReadOnlyList<Match<TValue, TCandidate>> Match(
        IReadOnlyList<TValue> values,
        IReadOnlyList<TCandidate> candidates,
        Func<TValue, object> valueTransform,
        Func<TCandidate, object> candidateTransform)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(valueTransform);
        ArgumentNullException.ThrowIfNull(candidateTransform);

        // 1. Score all value×candidate pairs
        var pairs = new List<(int ValueIndex, int CandidateIndex, float Score)>(values.Count * candidates.Count);

        for (var vi = 0; vi < values.Count; vi++)
        {
            var vObj = valueTransform(values[vi]);
            for (var ci = 0; ci < candidates.Count; ci++)
            {
                var cObj = candidateTransform(candidates[ci]);
                var score = ComputeScore(vObj, cObj);
                if (score >= _threshold)
                {
                    pairs.Add((vi, ci, score));
                }
            }
        }

        // 2. Sort by score descending (greedy best-first)
        pairs.Sort((x, y) => y.Score.CompareTo(x.Score));

        // 3. Greedily select disjoint matches
        var usedValues = new HashSet<int>();
        var usedCandidates = new HashSet<int>();
        var results = new List<Match<TValue, TCandidate>>();

        foreach (var (valueIndex, candidateIndex, score) in pairs)
        {
            if (usedValues.Contains(valueIndex) || usedCandidates.Contains(candidateIndex))
                continue;

            usedValues.Add(valueIndex);
            usedCandidates.Add(candidateIndex);
            results.Add(new Match<TValue, TCandidate>(values[valueIndex], candidates[candidateIndex], score));
        }

        return results;
    }

    private float ComputeScore(object? a, object? b)
    {
        if (_metrics.Count == 0)
            return 0.0f;

        // Use the maximum score across all metrics
        var best = 0.0f;
        foreach (var metric in _metrics)
        {
            var score = metric.GetSimilarity(a, b);
            if (score > best)
                best = score;
        }

        return best;
    }
}
