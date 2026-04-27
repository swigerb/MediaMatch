using System.Threading.Channels;

namespace MediaMatch.Core.Services;

/// <summary>
/// Scans directories for media files using parallel enumeration
/// with streaming results via <see cref="ChannelReader{T}"/>.
/// </summary>
public interface IParallelFileScanner
{
    /// <summary>
    /// Scans the specified directory for files matching the given extensions.
    /// Returns a channel reader that streams discovered file paths.
    /// </summary>
    ChannelReader<string> ScanAsync(
        string rootPath,
        IReadOnlySet<string>? allowedExtensions = null,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Scans and collects all results into a list. Convenience wrapper over <see cref="ScanAsync"/>.
    /// </summary>
    Task<IReadOnlyList<string>> ScanToListAsync(
        string rootPath,
        IReadOnlySet<string>? allowedExtensions = null,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default);
}

/// <summary>
/// Progress report for parallel file scanning operations.
/// </summary>
/// <param name="FilesFound">Total files discovered so far.</param>
/// <param name="FilesProcessed">Files that have been fully processed.</param>
/// <param name="CurrentFile">Path of the file currently being processed.</param>
/// <param name="ElapsedMs">Elapsed time in milliseconds since scan started.</param>
public sealed record ScanProgress(
    int FilesFound,
    int FilesProcessed,
    string? CurrentFile,
    long ElapsedMs);
