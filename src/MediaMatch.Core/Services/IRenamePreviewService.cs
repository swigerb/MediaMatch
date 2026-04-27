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
    Task<IReadOnlyList<FileOrganizationResult>> PreviewAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        CancellationToken ct = default);
}
