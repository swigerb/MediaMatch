using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.App.Dialogs;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Home page — manages file list, folder selection, batch rename, and undo.
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly IBatchOperationService? _batchService;
    private readonly IUndoService? _undoService;
    private readonly ILogger<HomeViewModel> _logger;
    private CancellationTokenSource? _batchCts;

    public ObservableCollection<FileItemViewModel> Files { get; } = [];

    public BatchProgressViewModel BatchProgress { get; } = new();

    [ObservableProperty]
    public partial string SelectedFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "No files loaded. Add a folder to get started.";

    [ObservableProperty]
    public partial bool CanUndo { get; set; }

    public int FileCount => Files.Count;
    public int SelectedCount => Files.Count(f => f.IsSelected);
    public bool HasFiles => Files.Count > 0;
    public bool HasNoFiles => Files.Count == 0;

    /// <summary>
    /// Design-time / test constructor (no services).
    /// </summary>
    public HomeViewModel() : this(null, null, null) { }

    public HomeViewModel(
        IBatchOperationService? batchService,
        IUndoService? undoService,
        ILogger<HomeViewModel>? logger)
    {
        _batchService = batchService;
        _undoService = undoService;
        _logger = logger ?? NullLogger<HomeViewModel>.Instance;

        Files.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FileCount));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(HasNoFiles));
            UpdateStatusMessage();
        };

        _ = RefreshCanUndoAsync();
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
        StatusMessage = $"Scanning {folder.Name}...";

        try
        {
            await ScanFolderAsync(folder.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan folder {Folder}", folder.Path);
            StatusMessage = $"Error scanning folder: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            UpdateStatusMessage();
        }
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        var selected = Files.Where(f => f.IsSelected).ToList();
        foreach (var file in selected)
        {
            Files.Remove(file);
        }
        UpdateStatusMessage();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var file in Files)
        {
            file.IsSelected = true;
        }
        OnPropertyChanged(nameof(SelectedCount));
    }

    [RelayCommand]
    private async Task ApplyRenamesAsync()
    {
        if (Files.Count == 0) return;

        if (_batchService is null)
        {
            StatusMessage = "Batch service not available.";
            return;
        }

        IsProcessing = true;
        BatchProgress.IsRunning = true;
        _batchCts = new CancellationTokenSource();

        var filePaths = Files.Select(f => f.FilePath).ToList();
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
            // Default pattern — will use settings in future
            var job = await _batchService.ExecuteAsync(filePaths, "{n}", progress, _batchCts.Token);

            // Record successful renames for undo
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch rename failed");
            StatusMessage = $"Rename error: {ex.Message}";
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

        Files.Clear();
        IsProcessing = true;
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
            UpdateStatusMessage();
        }
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
                    MediaType = GetMediaType(file.Extension),
                    MatchConfidence = 0
                };

                App.MainWindow.DispatcherQueue.TryEnqueue(() => Files.Add(item));
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

    private void UpdateStatusMessage()
    {
        if (Files.Count == 0)
        {
            StatusMessage = "No files loaded. Add a folder to get started.";
        }
        else
        {
            StatusMessage = $"{Files.Count} file(s) loaded";
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
    /// Returns the user's chosen resolution.
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
    /// Returns the selected match, or null if the user skipped.
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
