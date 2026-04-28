namespace MediaMatch.Core.Services;

/// <summary>
/// Abstraction for LLM providers used in AI-assisted renaming.
/// </summary>
public interface ILlmProvider
{
    /// <summary>Provider name for settings (e.g. "OpenAI", "AzureOpenAI", "Ollama").</summary>
    string Name { get; }

    /// <summary>Whether the provider is configured and reachable.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Sends a prompt to the LLM and returns a suggested filename.
    /// </summary>
    /// <param name="prompt">The prompt text to send to the LLM.</param>
    /// <param name="context">The media context for the file being renamed.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The suggested filename.</returns>
    Task<string> GenerateRenameAsync(string prompt, MediaContext context, CancellationToken ct = default);
}

/// <summary>
/// Context about the media file being renamed, passed to the LLM.
/// </summary>
public sealed record MediaContext(
    string OriginalFileName,
    string? DetectedType,
    string? MatchedTitle,
    int? Season,
    int? Episode,
    int? Year,
    string? Quality,
    string? ReleaseGroup);
