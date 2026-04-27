namespace MediaMatch.Core.Models;

/// <summary>
/// A candidate match suggestion returned by opportunistic matching
/// when strict matching (≥0.85 confidence) fails.
/// </summary>
public sealed record MatchSuggestion(
    string ProviderName,
    double Confidence,
    string Title,
    int? Year,
    string? MetadataSummary,
    string? ProviderId);
