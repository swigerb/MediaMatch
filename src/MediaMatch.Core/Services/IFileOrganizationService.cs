using MediaMatch.Core.Models;

namespace MediaMatch.Core.Services;

/// <summary>
/// Orchestrates the full rename/move workflow for media files.
/// </summary>
public interface IFileOrganizationService
{
    /// <summary>
    /// Organize a batch of files: detect media type, match metadata, generate new names, and apply renames.
    /// </summary>
    /// <param name="filePaths">The file paths to organize.</param>
    /// <param name="renamePattern">The naming pattern to apply.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of organization results.</returns>
    Task<IReadOnlyList<FileOrganizationResult>> OrganizeAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        CancellationToken ct = default);

    /// <summary>
    /// Organize files with a specific rename action (move, copy, hardlink, etc.).
    /// </summary>
    /// <param name="filePaths">The file paths to organize.</param>
    /// <param name="renamePattern">The naming pattern to apply.</param>
    /// <param name="action">The rename action to perform.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of organization results.</returns>
    Task<IReadOnlyList<FileOrganizationResult>> OrganizeAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        Core.Enums.RenameAction action,
        CancellationToken ct = default);
}
