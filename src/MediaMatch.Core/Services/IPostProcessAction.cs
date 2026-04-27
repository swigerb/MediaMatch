using MediaMatch.Core.Models;

namespace MediaMatch.Core.Services;

/// <summary>
/// Post-processing action that runs after a successful rename operation.
/// </summary>
public interface IPostProcessAction
{
    /// <summary>Display name of this action (e.g., "plex-refresh").</summary>
    string Name { get; }

    /// <summary>
    /// Execute the post-processing action for a completed rename.
    /// </summary>
    Task ExecuteAsync(FileOrganizationResult result, CancellationToken ct = default);

    /// <summary>
    /// Whether this action's prerequisites are met (e.g., Plex is reachable).
    /// </summary>
    bool IsAvailable { get; }
}
