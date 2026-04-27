using MediaMatch.Core.Models;

namespace MediaMatch.Core.Services;

/// <summary>
/// Coordinates the matching pipeline: detect media type → match against providers → generate rename result.
/// </summary>
public interface IMatchingPipeline
{
    /// <summary>
    /// Run the full matching pipeline for a single file.
    /// </summary>
    Task<MatchResult> ProcessAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Run the matching pipeline for a batch of files.
    /// </summary>
    Task<IReadOnlyList<MatchResult>> ProcessBatchAsync(
        IReadOnlyList<string> filePaths,
        CancellationToken ct = default);
}
