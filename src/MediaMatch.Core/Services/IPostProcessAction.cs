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
    /// <param name="result">The file organization result to process.</param>
    /// <param name="ct">A cancellation token.</param>
    Task ExecuteAsync(FileOrganizationResult result, CancellationToken ct = default);

    /// <summary>Gets a value indicating whether this action's prerequisites are met.</summary>
    bool IsAvailable { get; }
}
