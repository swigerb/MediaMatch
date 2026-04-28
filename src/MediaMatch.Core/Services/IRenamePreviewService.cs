using MediaMatch.Core.Models;

namespace MediaMatch.Core.Services;

/// <summary>
/// Generates a preview of rename operations without modifying the file system.
/// </summary>
public interface IRenamePreviewService
{
    /// <summary>
    /// Preview what files would look like after applying a rename pattern.
    /// No file system changes are made.
    /// </summary>
    /// <param name="filePaths">The file paths to preview.</param>
    /// <param name="renamePattern">The naming pattern to apply.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of preview results.</returns>
    Task<IReadOnlyList<FileOrganizationResult>> PreviewAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        CancellationToken ct = default);
}
