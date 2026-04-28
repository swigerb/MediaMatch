using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.App.Dialogs;
using MediaMatch.App.Services;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Home page — manages dual-pane file matching, rename, and undo.
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly IBatchOperationService? _batchService;
    private readonly IUndoService? _undoService;
    private readonly IMatchingPipeline? _matchingPipeline;
    private readonly ILogger<HomeViewModel> _logger;
    private CancellationTokenSource? _batchCts;
    private NotificationService? _notificationService;

    /// <summary>Left pane: original files loaded by the user.</summary>
    public ObservableCollection<FileItemViewModel> OriginalFiles { get; } = [];

    /// <summary>Right pane: matched/renamed file previews.</summary>
    public ObservableCollection<FileItemViewModel> MatchedFiles { get; } = [];

    /// <summary>Legacy single collection for backward compatibility with tests.</summary>
    public ObservableCollection<FileItemViewModel> Files => OriginalFiles;

    public BatchProgressViewModel BatchProgress { get; } = new();

    [ObservableProperty]
    public partial string SelectedFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    [ObservableProperty]
    public partial bool IsScanning { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "No files loaded. Drop files or click Load.";

    [ObservableProperty]
    public partial bool CanUndo { get; set; }

    [ObservableProperty]
    public partial int SelectedModeIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedRenameActionIndex { get; set; }

    public int FileCount => OriginalFiles.Count;
    public int MatchedCount => MatchedFiles.Count;
    public bool HasFiles => OriginalFiles.Count > 0;
    public bool HasNoFiles => OriginalFiles.Count == 0;
    public bool HasMatchedFiles => MatchedFiles.Count > 0;
    public bool HasNoMatchedFiles => MatchedFiles.Count == 0;
    public bool ShowEmptyState => HasNoFiles && !IsScanning;
    public string FileCountDisplay => $"{OriginalFiles.Count} file(s) | {MatchedFiles.Count} matched";

    public string[] RenameActionOptions { get; } = ["Move", "Copy", "Hard Link", "Symlink", "Test"];

    /// <summary>
    /// Wires the notification service for operation feedback.
    /// </summary>
    public void SetNotificationService(NotificationService service)
    {
        _notificationService = service;
    }

    /// <summary>
    /// Design-time / test constructor (no services).
    /// </summary>
    public HomeViewModel() : this(null, null, null, null) { }

    public HomeViewModel(
        IBatchOperationService? batchService,
        IUndoService? undoService,
        IMatchingPipeline? matchingPipeline,
        ILogger<HomeViewModel>? logger)
    {
        _batchService = batchService;
        _undoService = undoService;
        _matchingPipeline = matchingPipeline;
        _logger = logger ?? NullLogger<HomeViewModel>.Instance;

        OriginalFiles.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FileCount));
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(HasNoFiles));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(FileCountDisplay));
            UpdateStatusMessage();
        };

        MatchedFiles.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(MatchedCount));
            OnPropertyChanged(nameof(HasMatchedFiles));
            OnPropertyChanged(nameof(HasNoMatchedFiles));
            OnPropertyChanged(nameof(FileCountDisplay));
        };

        _ = RefreshCanUndoAsync();
    }

    partial void OnIsScanningChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null) return;

        SelectedFolder = folder.Path;
        IsProcessing = true;
        IsScanning = true;
        StatusMessage = $"Scanning {folder.Name}...";

        try
        {
            await ScanFolderAsync(folder.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan folder {Folder}", folder.Path);
            StatusMessage = $"Error scanning folder: {ex.Message}";
            _notificationService?.ShowError($"Failed to scan folder: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            IsScanning = false;
            UpdateStatusMessage();
        }
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        var selected = OriginalFiles.Where(f => f.IsSelected).ToList();
        foreach (var file in selected)
        {
            OriginalFiles.Remove(file);
        }

        // Remove corresponding matched entries
        var selectedPaths = selected.Select(f => f.FilePath).ToHashSet();
        var matchedToRemove = MatchedFiles.Where(f => selectedPaths.Contains(f.FilePath)).ToList();
        foreach (var file in matchedToRemove)
        {
            MatchedFiles.Remove(file);
        }

        UpdateStatusMessage();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var file in OriginalFiles)
        {
            file.IsSelected = true;
        }
    }

    [RelayCommand]
    private async Task MatchAsync()
    {
        if (OriginalFiles.Count == 0) return;

        if (_matchingPipeline is null)
        {
            StatusMessage = "Matching pipeline not available.";
            return;
        }

        IsProcessing = true;
        StatusMessage = "Matching files against metadata providers...";
        MatchedFiles.Clear();

        try
        {
            var filePaths = OriginalFiles.Select(f => f.FilePath).ToList();
            var results = await _matchingPipeline.ProcessBatchAsync(filePaths);

            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var original = i < OriginalFiles.Count ? OriginalFiles[i] : null;

                var newName = result.IsMatch
                    ? BuildNewFileName(result, original?.OriginalFileName ?? string.Empty)
                    : original?.OriginalFileName ?? string.Empty;

                var matchedItem = new FileItemViewModel
                {
                    OriginalFileName = original?.OriginalFileName ?? string.Empty,
                    NewFileName = newName,
                    FilePath = original?.FilePath ?? string.Empty,
                    FileExtension = original?.FileExtension ?? string.Empty,
                    MatchConfidence = result.Confidence,
                    MediaType = result.MediaType.ToString(),
                    ProviderSource = result.ProviderSource,
                    IsMatched = result.IsMatch
                };

                App.MainWindow.DispatcherQueue.TryEnqueue(() => MatchedFiles.Add(matchedItem));
            }

            var matchCount = results.Count(r => r.IsMatch);
            StatusMessage = $"Matched {matchCount}/{results.Count} file(s).";
            _notificationService?.ShowSuccess($"Matched {matchCount} of {results.Count} file(s).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Matching failed");
            StatusMessage = $"Match error: {ex.Message}";
            _notificationService?.ShowError($"Match error: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task RenameAsync()
    {
        if (MatchedFiles.Count == 0) return;

        if (_batchService is null)
        {
            StatusMessage = "Batch service not available.";
            return;
        }

        IsProcessing = true;
        BatchProgress.IsRunning = true;
        _batchCts = new CancellationTokenSource();

        var filePaths = OriginalFiles.Select(f => f.FilePath).ToList();
        var progress = new Progress<BatchProgress>(p =>
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                BatchProgress.Update(p.TotalFiles, p.CompletedFiles, p.FailedFiles, p.CurrentFile);
                StatusMessage = $"Renaming: {p.CompletedFiles + p.FailedFiles}/{p.TotalFiles}...";
            });
        });

        try
        {
            var job = await _batchService.ExecuteAsync(filePaths, "{n}", progress, _batchCts.Token);

            if (_undoService is not null)
            {
                var undoEntries = job.Files
                    .Where(f => f.Status == BatchFileStatus.Success && f.NewPath is not null)
                    .Select(f => new UndoEntry(
                        f.FilePath,
                        f.NewPath!,
                        DateTimeOffset.UtcNow,
                        MediaType.Unknown))
                    .ToList();

                if (undoEntries.Count > 0)
                {
                    await _undoService.RecordAsync(undoEntries);
                }
            }

            StatusMessage = job.Status switch
            {
                BatchStatus.Completed => $"Done — {job.CompletedCount} renamed, {job.FailedCount} failed.",
                BatchStatus.Cancelled => $"Cancelled — {job.CompletedCount} renamed before cancellation.",
                BatchStatus.Failed => "Batch operation failed.",
                _ => "Renames completed."
            };

            if (job.Status == BatchStatus.Completed && job.FailedCount == 0)
                _notificationService?.ShowSuccess($"Batch complete — {job.CompletedCount} file(s) renamed.");
            else if (job.Status == BatchStatus.Completed && job.FailedCount > 0)
                _notificationService?.ShowError($"Batch finished with {job.FailedCount} error(s).");
            else if (job.Status == BatchStatus.Cancelled)
                _notificationService?.ShowInfo($"Batch cancelled — {job.CompletedCount} renamed before stop.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch rename failed");
            StatusMessage = $"Rename error: {ex.Message}";
            _notificationService?.ShowError($"Rename error: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            BatchProgress.IsRunning = false;
            _batchCts?.Dispose();
            _batchCts = null;
            await RefreshCanUndoAsync();
        }
    }

    /// <summary>Alias for backward compatibility with existing tests and XAML.</summary>
    [RelayCommand]
    private Task ApplyRenamesAsync() => RenameAsync();

    [RelayCommand]
    private void CancelBatch()
    {
        _batchCts?.Cancel();
        StatusMessage = "Cancelling...";
    }

    [RelayCommand]
    private async Task UndoLastAsync()
    {
        if (_undoService is null)
        {
            StatusMessage = "Undo service not available.";
            return;
        }

        IsProcessing = true;
        try
        {
            int undone = await _undoService.UndoAsync(1);
            StatusMessage = undone > 0
                ? $"Undid {undone} rename operation(s)."
                : "Nothing to undo.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Undo failed");
            StatusMessage = $"Undo error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            await RefreshCanUndoAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (string.IsNullOrEmpty(SelectedFolder)) return;

        OriginalFiles.Clear();
        MatchedFiles.Clear();
        IsProcessing = true;
        IsScanning = true;
        StatusMessage = "Refreshing...";

        try
        {
            await ScanFolderAsync(SelectedFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh failed");
            StatusMessage = $"Refresh error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            IsScanning = false;
            UpdateStatusMessage();
        }
    }

    [RelayCommand]
    private void SortOriginalFiles()
    {
        var sorted = OriginalFiles.OrderBy(f => f.OriginalFileName).ToList();
        OriginalFiles.Clear();
        foreach (var file in sorted)
        {
            OriginalFiles.Add(file);
        }
    }

    [RelayCommand]
    private void SortMatchedFiles()
    {
        var sorted = MatchedFiles.OrderBy(f => f.NewFileName).ToList();
        MatchedFiles.Clear();
        foreach (var file in sorted)
        {
            MatchedFiles.Add(file);
        }
    }

    [RelayCommand]
    private void ClearMatches()
    {
        MatchedFiles.Clear();
        StatusMessage = "Matches cleared.";
    }

    [RelayCommand]
    private async Task FetchDataAsync()
    {
        // Re-match to refresh metadata from providers
        await MatchAsync();
    }

    [RelayCommand]
    private void PasteFiles()
    {
        // Placeholder for clipboard paste — will be wired in Phase 2
        _notificationService?.ShowInfo("Clipboard paste coming soon.");
    }

    /// <summary>
    /// Adds files to the original files pane programmatically (for drag-and-drop).
    /// </summary>
    public void AddFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists) continue;

            var item = new FileItemViewModel
            {
                OriginalFileName = fileInfo.Name,
                NewFileName = fileInfo.Name,
                FilePath = fileInfo.FullName,
                FileExtension = fileInfo.Extension.TrimStart('.').ToLowerInvariant(),
                OriginalFolder = fileInfo.DirectoryName ?? string.Empty,
                MediaType = GetMediaType(fileInfo.Extension),
                MatchConfidence = 0
            };

            OriginalFiles.Add(item);
        }

        UpdateStatusMessage();
    }

    private async Task ScanFolderAsync(string folderPath)
    {
        var mediaExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".m4v",
            ".srt", ".sub", ".idx", ".ssa", ".ass"
        };

        await Task.Run(() =>
        {
            var dir = new DirectoryInfo(folderPath);
            if (!dir.Exists) return;

            var mediaFiles = dir.EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(f => mediaExtensions.Contains(f.Extension));

            foreach (var file in mediaFiles)
            {
                var item = new FileItemViewModel
                {
                    OriginalFileName = file.Name,
                    NewFileName = file.Name,
                    FilePath = file.FullName,
                    FileExtension = file.Extension.TrimStart('.').ToLowerInvariant(),
                    OriginalFolder = file.DirectoryName ?? string.Empty,
                    MediaType = GetMediaType(file.Extension),
                    MatchConfidence = 0
                };

                App.MainWindow.DispatcherQueue.TryEnqueue(() => OriginalFiles.Add(item));
            }
        });
    }

    private static string GetMediaType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".srt" or ".sub" or ".idx" or ".ssa" or ".ass" => "Subtitle",
            _ => "Video"
        };
    }

    private static string BuildNewFileName(MatchResult result, string originalFileName)
    {
        // Build a display name from match result metadata
        var ext = Path.GetExtension(originalFileName);

        if (result.Movie is not null)
        {
            var year = result.MovieInfo?.Year > 0 ? $" ({result.MovieInfo.Year})" : string.Empty;
            return $"{result.Movie.Name}{year}{ext}";
        }

        if (result.Episode is not null)
        {
            var series = result.Episode.SeriesName;
            var s = result.Episode.Season;
            var e = result.Episode.EpisodeNumber;
            var title = result.Episode.Title;
            return $"{series} - S{s:D2}E{e:D2} - {title}{ext}";
        }

        return originalFileName;
    }

    private void UpdateStatusMessage()
    {
        if (OriginalFiles.Count == 0)
        {
            StatusMessage = "No files loaded. Drop files or click Load.";
        }
        else
        {
            StatusMessage = $"{OriginalFiles.Count} file(s) loaded";
        }
    }

    private async Task RefreshCanUndoAsync()
    {
        if (_undoService is not null)
        {
            try
            {
                CanUndo = await _undoService.CanUndoAsync();
            }
            catch
            {
                CanUndo = false;
            }
        }
    }

    /// <summary>
    /// Shows the conflict resolution dialog when a target file already exists.
    /// </summary>
    public async Task<ConflictResolution> ShowConflictDialogAsync(string sourcePath, string targetPath)
    {
        var vm = new ConflictDialogViewModel();
        vm.LoadFromFiles(sourcePath, targetPath);

        var dialog = new ConflictDialog(vm)
        {
            XamlRoot = App.MainWindow.Content.XamlRoot
        };

        await dialog.ShowAsync();
        return vm.Resolution;
    }

    /// <summary>
    /// Shows the match selection dialog when opportunistic matching returns candidates.
    /// </summary>
    public async Task<MatchSuggestion?> ShowMatchSelectionDialogAsync(
        string fileName,
        IEnumerable<MatchSuggestion> suggestions)
    {
        var vm = new MatchSelectionViewModel();
        vm.LoadSuggestions(fileName, suggestions);

        var dialog = new MatchSelectionDialog(vm)
        {
            XamlRoot = App.MainWindow.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? vm.SelectedMatch : null;
    }
}
