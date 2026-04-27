using System.Diagnostics;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Services;

/// <summary>
/// Executes configured post-processing actions after each successful rename.
/// Individual action failures are caught and logged — one failure doesn't stop others.
/// </summary>
public sealed class PostProcessPipeline
{
    private static readonly ActivitySource ActivitySrc = new("MediaMatch", "0.1.0");

    private readonly IReadOnlyList<IPostProcessAction> _actions;
    private readonly ILogger<PostProcessPipeline> _logger;

    public PostProcessPipeline(
        IEnumerable<IPostProcessAction> actions,
        ILogger<PostProcessPipeline>? logger = null)
    {
        _actions = actions.ToList();
        _logger = logger ?? NullLogger<PostProcessPipeline>.Instance;
    }

    /// <summary>
    /// Execute all configured and available actions for a rename result.
    /// </summary>
    public async Task ExecuteAsync(FileOrganizationResult result, CancellationToken ct = default)
    {
        await ExecuteAsync(result, actionFilter: null, ct);
    }

    /// <summary>
    /// Execute specific named actions for a rename result.
    /// </summary>
    public async Task ExecuteAsync(FileOrganizationResult result, IReadOnlySet<string>? actionFilter, CancellationToken ct = default)
    {
        foreach (var action in _actions)
        {
            ct.ThrowIfCancellationRequested();

            // Filter by name if specified
            if (actionFilter is not null && !actionFilter.Contains(action.Name))
                continue;

            if (!action.IsAvailable)
            {
                _logger.LogDebug("Post-process action {Action} is not available, skipping", action.Name);
                continue;
            }

            using var activity = ActivitySrc.StartActivity($"mediamatch.postprocess.{action.Name}");
            activity?.SetTag("mediamatch.action", action.Name);
            activity?.SetTag("mediamatch.file", result.NewPath ?? result.OriginalPath);

            try
            {
                _logger.LogInformation("Running post-process action: {Action}", action.Name);
                await action.ExecuteAsync(result, ct);
                activity?.SetTag("mediamatch.action.success", true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Post-process action {Action} failed", action.Name);
                activity?.SetTag("mediamatch.action.success", false);
                activity?.SetTag("mediamatch.action.error", ex.Message);
                // Continue to next action — don't stop pipeline
            }
        }
    }
}
