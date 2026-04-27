using System.Diagnostics;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Services;

/// <summary>
/// Builds context from detected media info, sends to the selected LLM provider,
/// and returns AI-suggested filenames alongside pattern-based suggestions.
/// </summary>
public sealed class AiRenameService : IAiRenameService
{
    private static readonly ActivitySource Activity = new("MediaMatch", "0.1.0");

    private readonly IEnumerable<ILlmProvider> _providers;
    private readonly ILogger<AiRenameService> _logger;

    public AiRenameService(
        IEnumerable<ILlmProvider> providers,
        ILogger<AiRenameService>? logger = null)
    {
        _providers = providers;
        _logger = logger ?? NullLogger<AiRenameService>.Instance;
    }

    public async Task<AiRenameSuggestion?> SuggestRenameAsync(MediaContext context, CancellationToken ct = default)
    {
        using var activity = Activity.StartActivity("mediamatch.ai.rename");

        var provider = _providers.FirstOrDefault(p => p.IsAvailable);
        if (provider is null)
        {
            _logger.LogDebug("No LLM provider available for AI rename");
            return null;
        }

        activity?.SetTag("mediamatch.ai.provider", provider.Name);
        _logger.LogInformation("Using LLM provider {Provider} for AI rename", provider.Name);

        var prompt = BuildPrompt(context);
        var sw = Stopwatch.StartNew();

        try
        {
            var suggestion = await provider.GenerateRenameAsync(prompt, context, ct);
            sw.Stop();

            if (string.IsNullOrWhiteSpace(suggestion))
            {
                _logger.LogWarning("LLM provider {Provider} returned empty suggestion", provider.Name);
                return null;
            }

            // Sanitize: strip quotes, newlines, path separators
            suggestion = suggestion
                .Trim('"', '\'', '`')
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();

            _logger.LogInformation("AI rename suggestion from {Provider}: {Suggestion} ({Elapsed}ms)",
                provider.Name, suggestion, sw.ElapsedMilliseconds);

            return new AiRenameSuggestion(suggestion, provider.Name, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "LLM provider {Provider} failed after {Elapsed}ms", provider.Name, sw.ElapsedMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return null;
        }
    }

    private static string BuildPrompt(MediaContext context)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Original filename: {context.OriginalFileName}");

        if (!string.IsNullOrWhiteSpace(context.DetectedType))
            sb.AppendLine($"Detected type: {context.DetectedType}");
        if (!string.IsNullOrWhiteSpace(context.MatchedTitle))
            sb.AppendLine($"Matched title: {context.MatchedTitle}");
        if (context.Season.HasValue)
            sb.AppendLine($"Season: {context.Season}");
        if (context.Episode.HasValue)
            sb.AppendLine($"Episode: {context.Episode}");
        if (context.Year.HasValue)
            sb.AppendLine($"Year: {context.Year}");
        if (!string.IsNullOrWhiteSpace(context.Quality))
            sb.AppendLine($"Quality: {context.Quality}");
        if (!string.IsNullOrWhiteSpace(context.ReleaseGroup))
            sb.AppendLine($"Release group: {context.ReleaseGroup}");

        sb.AppendLine();
        sb.AppendLine("Suggest a clean, properly formatted filename (include the file extension).");

        return sb.ToString();
    }
}
