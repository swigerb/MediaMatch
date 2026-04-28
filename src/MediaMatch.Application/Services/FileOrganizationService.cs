using System.Diagnostics;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Expressions;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using MediaMatch.Application.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Services;

/// <summary>
/// Orchestrates the full rename/move workflow for media files.
/// Detects media type, matches metadata, generates new names, and applies renames
/// with rollback on failure.
/// </summary>
public sealed class FileOrganizationService : IFileOrganizationService
{
    private static readonly ActivitySource Activity = new("MediaMatch", "0.1.0");

    private readonly IRenamePreviewService _previewService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FileOrganizationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOrganizationService"/> class.
    /// </summary>
    /// <param name="previewService">The service used to generate rename previews.</param>
    /// <param name="fileSystem">The file system abstraction for rename operations.</param>
    /// <param name="logger">Optional logger instance.</param>
    public FileOrganizationService(
        IRenamePreviewService previewService,
        IFileSystem fileSystem,
        ILogger<FileOrganizationService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(previewService);
        ArgumentNullException.ThrowIfNull(fileSystem);

        _previewService = previewService;
        _fileSystem = fileSystem;
        _logger = logger ?? NullLogger<FileOrganizationService>.Instance;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<FileOrganizationResult>> OrganizeAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        CancellationToken ct = default)
    {
        return OrganizeAsync(filePaths, renamePattern, RenameAction.Move, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileOrganizationResult>> OrganizeAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        RenameAction action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        ArgumentException.ThrowIfNullOrWhiteSpace(renamePattern);

        if (filePaths.Count == 0)
            return [];

        using var activity = Activity.StartActivity("mediamatch.rename");
        activity?.SetTag("mediamatch.file_count", filePaths.Count);
        activity?.SetTag("mediamatch.action", action.ToString());

        _logger.LogInformation("Organizing {FileCount} files with action={Action}", filePaths.Count, action);

        // 1. Generate preview (detect + match + template)
        var previews = await _previewService.PreviewAsync(filePaths, renamePattern, ct).ConfigureAwait(false);

        // 2. Apply renames with rollback tracking
        if (action == RenameAction.Test)
        {
            _logger.LogDebug("Test mode — returning {PreviewCount} previews without applying", previews.Count);
            return previews;
        }

        var completed = new List<(string From, string To)>();
        var finalResults = new List<FileOrganizationResult>(previews.Count);

        try
        {
            foreach (var preview in previews)
            {
                ct.ThrowIfCancellationRequested();

                if (!preview.Success || string.IsNullOrEmpty(preview.NewPath))
                {
                    finalResults.Add(preview);
                    continue;
                }

                // Skip if source and destination are the same
                if (string.Equals(preview.OriginalPath, preview.NewPath, StringComparison.OrdinalIgnoreCase))
                {
                    finalResults.Add(preview with
                    {
                        Warnings = [.. preview.Warnings, "Source and destination are identical — skipped"]
                    });
                    continue;
                }

                try
                {
                    await ApplyRenameAsync(preview.OriginalPath, preview.NewPath, action).ConfigureAwait(false);
                    completed.Add((preview.OriginalPath, preview.NewPath));
                    finalResults.Add(preview);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rename failed for file, rolling back {CompletedCount} operations", completed.Count);

                    // Rollback all completed renames on failure
                    await RollbackAsync(completed).ConfigureAwait(false);

                    finalResults.Add(FileOrganizationResult.Failed(
                        preview.OriginalPath,
                        $"Rename failed: {ex.Message}. All previous renames rolled back."));

                    // Mark remaining as not attempted
                    for (int i = finalResults.Count; i < previews.Count; i++)
                    {
                        finalResults.Add(FileOrganizationResult.Failed(
                            previews[i].OriginalPath,
                            "Skipped due to earlier failure"));
                    }

                    return finalResults;
                }
            }
        }
        catch (OperationCanceledException)
        {
            await RollbackAsync(completed).ConfigureAwait(false);
            throw;
        }

        return finalResults;
    }

    private async Task ApplyRenameAsync(string source, string destination, RenameAction action)
    {
        // Ensure destination directory exists
        var destDir = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(destDir))
            _fileSystem.CreateDirectory(destDir);

        switch (action)
        {
            case RenameAction.Move:
                _fileSystem.MoveFile(source, destination);
                break;
            case RenameAction.Copy:
                _fileSystem.CopyFile(source, destination);
                break;
            case RenameAction.Hardlink:
                _fileSystem.CreateHardLink(destination, source);
                break;
            case RenameAction.Clone:
                await _fileSystem.CloneFileAsync(source, destination).ConfigureAwait(false);
                break;
            default:
                _fileSystem.MoveFile(source, destination);
                break;
        }
    }

    private Task RollbackAsync(List<(string From, string To)> completed)
    {
        // Rollback in reverse order
        for (int i = completed.Count - 1; i >= 0; i--)
        {
            try
            {
                var (from, to) = completed[i];
                if (_fileSystem.FileExists(to))
                    _fileSystem.MoveFile(to, from);
            }
            catch
            {
                // Best-effort rollback — log but don't throw
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Abstraction over file system operations to enable testing.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Checks whether a file exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Moves a file from one location to another.
    /// </summary>
    /// <param name="source">The source file path.</param>
    /// <param name="destination">The destination file path.</param>
    void MoveFile(string source, string destination);

    /// <summary>
    /// Copies a file from one location to another.
    /// </summary>
    /// <param name="source">The source file path.</param>
    /// <param name="destination">The destination file path.</param>
    void CopyFile(string source, string destination);

    /// <summary>
    /// Creates a hard link at the specified path pointing to the target file.
    /// </summary>
    /// <param name="linkPath">The path for the new hard link.</param>
    /// <param name="targetPath">The path to the existing target file.</param>
    void CreateHardLink(string linkPath, string targetPath);

    /// <summary>
    /// Creates a directory at the specified path, including any intermediate directories.
    /// </summary>
    /// <param name="path">The directory path to create.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Clones a file using the best available method (CoW → hardlink → copy).
    /// </summary>
    Task CloneFileAsync(string source, string destination, CancellationToken ct = default);
}

/// <summary>
/// Default implementation that delegates to System.IO.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    /// <inheritdoc/>
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc/>
    public void MoveFile(string source, string destination) => File.Move(source, destination);

    /// <inheritdoc/>
    public void CopyFile(string source, string destination) => File.Copy(source, destination);

    /// <inheritdoc/>
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    /// <inheritdoc/>
    public void CreateHardLink(string linkPath, string targetPath)
    {
        // .NET doesn't have a built-in hard link API; fall back to copy
        File.Copy(targetPath, linkPath);
    }

    /// <inheritdoc/>
    public Task CloneFileAsync(string source, string destination, CancellationToken ct = default)
    {
        // Default implementation falls back to copy
        File.Copy(source, destination, overwrite: true);
        return Task.CompletedTask;
    }
}
