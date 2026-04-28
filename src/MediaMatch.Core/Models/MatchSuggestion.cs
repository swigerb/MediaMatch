namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a candidate match suggestion returned by opportunistic matching
/// when strict matching (≥0.85 confidence) fails.
/// </summary>
/// <param name="ProviderName">The name of the metadata provider that produced this suggestion.</param>
/// <param name="Confidence">The match confidence score between 0 and 1.</param>
/// <param name="Title">The suggested title from the provider.</param>
/// <param name="Year">The release year, or <c>null</c> if unavailable.</param>
/// <param name="MetadataSummary">A brief summary of the matched metadata for display.</param>
/// <param name="ProviderId">The provider-specific identifier for the suggested match.</param>
public sealed record MatchSuggestion(
    string ProviderName,
    double Confidence,
    string Title,
    int? Year,
    string? MetadataSummary,
    string? ProviderId);
