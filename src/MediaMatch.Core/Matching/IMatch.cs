namespace MediaMatch.Core.Matching;

/// <summary>
/// Represents a match between a source value and a candidate with a similarity score.
/// </summary>
/// <typeparam name="TValue">The type of the source value.</typeparam>
/// <typeparam name="TCandidate">The type of the matched candidate.</typeparam>
public interface IMatch<out TValue, out TCandidate>
{
    /// <summary>Gets the source value that was matched.</summary>
    TValue Value { get; }

    /// <summary>Gets the candidate that matched the source value.</summary>
    TCandidate Candidate { get; }

    /// <summary>Gets the similarity score between the value and candidate, from 0.0 to 1.0.</summary>
    float Score { get; }
}

/// <summary>
/// Default implementation of <see cref="IMatch{TValue, TCandidate}"/>.
/// </summary>
/// <typeparam name="TValue">The type of the source value.</typeparam>
/// <typeparam name="TCandidate">The type of the matched candidate.</typeparam>
public sealed record Match<TValue, TCandidate>(
    TValue Value,
    TCandidate Candidate,
    float Score) : IMatch<TValue, TCandidate>;
