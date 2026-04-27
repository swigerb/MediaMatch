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
    Task<IReadOnlyList<FileOrganizationResult>> OrganizeAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        CancellationToken ct = default);

    /// <summary>
    /// Organize files with a specific rename action (move, copy, hardlink, etc.)
    /// </summary>
    Task<IReadOnlyList<FileOrganizationResult>> OrganizeAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        Core.Enums.RenameAction action,
        CancellationToken ct = default);
}
