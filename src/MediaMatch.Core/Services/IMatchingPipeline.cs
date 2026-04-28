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
    /// <param name="filePath">The path to the media file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The match result for the file.</returns>
    Task<MatchResult> ProcessAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Run the matching pipeline for a batch of files.
    /// </summary>
    /// <param name="filePaths">The file paths to process.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of match results.</returns>
    Task<IReadOnlyList<MatchResult>> ProcessBatchAsync(
        IReadOnlyList<string> filePaths,
        CancellationToken ct = default);

    /// <summary>
    /// Run the matching pipeline for a batch of files using a specific datasource.
    /// </summary>
    /// <param name="filePaths">The file paths to process.</param>
    /// <param name="datasource">The datasource identifier (e.g., "tvdb", "tmdb", "auto").</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of match results.</returns>
    Task<IReadOnlyList<MatchResult>> ProcessBatchAsync(
        IReadOnlyList<string> filePaths,
        string datasource,
        CancellationToken ct = default);
}
