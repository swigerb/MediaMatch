namespace MediaMatch.Core.Services;

/// <summary>
/// Service that uses an LLM provider to suggest AI-assisted file renames.
/// </summary>
public interface IAiRenameService
{
    /// <summary>
    /// Generates an AI-suggested rename for the given media file context.
    /// Returns null if no LLM provider is configured or the provider is unavailable.
    /// </summary>
    Task<AiRenameSuggestion?> SuggestRenameAsync(MediaContext context, CancellationToken ct = default);
}

/// <summary>
/// AI-generated rename suggestion alongside the pattern-based suggestion.
/// </summary>
public sealed record AiRenameSuggestion(
    string SuggestedFileName,
    string ProviderName,
    TimeSpan Elapsed);
