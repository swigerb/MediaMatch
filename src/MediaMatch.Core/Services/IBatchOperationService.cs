using MediaMatch.Core.Models;

namespace MediaMatch.Core.Services;

/// <summary>
/// Processes multiple file renames in parallel with progress tracking and cancellation.
/// </summary>
public interface IBatchOperationService
{
    /// <summary>
    /// Execute a batch rename operation.
    /// </summary>
    /// <param name="filePaths">Files to rename.</param>
    /// <param name="renamePattern">The naming pattern to apply.</param>
    /// <param name="progress">Reports progress updates during the operation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The completed batch job with per-file results.</returns>
    Task<BatchJob> ExecuteAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default);
}

/// <summary>
/// Progress report for a running batch operation.
/// </summary>
public sealed record BatchProgress(
    int TotalFiles,
    int CompletedFiles,
    int FailedFiles,
    string? CurrentFile);
