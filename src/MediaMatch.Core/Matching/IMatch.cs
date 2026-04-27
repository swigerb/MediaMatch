namespace MediaMatch.Core.Matching;

public interface IMatch<out TValue, out TCandidate>
{
    TValue Value { get; }
    TCandidate Candidate { get; }
    float Score { get; }
}

public sealed record Match<TValue, TCandidate>(
    TValue Value,
    TCandidate Candidate,
    float Score) : IMatch<TValue, TCandidate>;
