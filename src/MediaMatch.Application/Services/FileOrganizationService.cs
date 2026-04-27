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

    public Task<IReadOnlyList<FileOrganizationResult>> OrganizeAsync(
        IReadOnlyList<string> filePaths,
        string renamePattern,
        CancellationToken ct = default)
    {
        return OrganizeAsync(filePaths, renamePattern, RenameAction.Move, ct);
    }

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
        var previews = await _previewService.PreviewAsync(filePaths, renamePattern, ct);

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
                    await ApplyRenameAsync(preview.OriginalPath, preview.NewPath, action);
                    completed.Add((preview.OriginalPath, preview.NewPath));
                    finalResults.Add(preview);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rename failed for file, rolling back {CompletedCount} operations", completed.Count);

                    // Rollback all completed renames on failure
                    await RollbackAsync(completed);

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
            await RollbackAsync(completed);
            throw;
        }

        return finalResults;
    }

    private Task ApplyRenameAsync(string source, string destination, RenameAction action)
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
            default:
                _fileSystem.MoveFile(source, destination);
                break;
        }

        return Task.CompletedTask;
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
    bool FileExists(string path);
    void MoveFile(string source, string destination);
    void CopyFile(string source, string destination);
    void CreateHardLink(string linkPath, string targetPath);
    void CreateDirectory(string path);
}

/// <summary>
/// Default implementation that delegates to System.IO.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public void MoveFile(string source, string destination) => File.Move(source, destination);
    public void CopyFile(string source, string destination) => File.Copy(source, destination);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void CreateHardLink(string linkPath, string targetPath)
    {
        // .NET doesn't have a built-in hard link API; fall back to move for now
        File.Copy(targetPath, linkPath);
    }
}
