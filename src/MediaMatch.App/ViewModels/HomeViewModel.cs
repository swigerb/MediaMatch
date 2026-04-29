using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.App.Dialogs;
using MediaMatch.App.Services;
using MediaMatch.Core.Configuration;
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
    private readonly ISettingsRepository? _settingsRepository;
    private readonly ILogger<HomeViewModel> _logger;
    private CancellationTokenSource? _batchCts;
    private NotificationService? _notificationService;

    /// <summary>Left pane: original files loaded by the user.</summary>
    public ObservableCollection<FileItemViewModel> OriginalFiles { get; } = [];

    /// <summary>Right pane: matched/renamed file previews.</summary>
    public ObservableCollection<FileItemViewModel> MatchedFiles { get; } = [];

    /// <summary>Legacy single collection for backward compatibility with tests.</summary>
    public ObservableCollection<FileItemViewModel> Files => OriginalFiles;

    /// <summary>Gets the batch progress tracker for rename operations.</summary>
    public BatchProgressViewModel BatchProgress { get; } = new();

    /// <summary>Gets or sets the currently selected folder path.</summary>
    [ObservableProperty]
    public partial string SelectedFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether a batch operation is in progress.</summary>
    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    /// <summary>Gets or sets a value indicating whether the folder scan is in progress.</summary>
    [ObservableProperty]
    public partial bool IsScanning { get; set; }

    /// <summary>Gets or sets the status bar message displayed to the user.</summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "No files loaded. Drop files or click Load.";

    /// <summary>Gets or sets a value indicating whether an undo operation is available.</summary>
    [ObservableProperty]
    public partial bool CanUndo { get; set; }

    /// <summary>Gets or sets the selected matching mode index.</summary>
    [ObservableProperty]
    public partial int SelectedModeIndex { get; set; }

    /// <summary>Gets or sets the selected rename action index.</summary>
    [ObservableProperty]
    public partial int SelectedRenameActionIndex { get; set; }

    /// <summary>Active match mode category: "none", "episode", "movie", "music", or "smart".</summary>
    [ObservableProperty]
    public partial string ActiveMatchMode { get; set; } = "none";

    /// <summary>Display label for the currently active datasource (e.g., "TheTVDB").</summary>
    [ObservableProperty]
    public partial string ActiveDatasourceLabel { get; set; } = string.Empty;

    /// <summary>Whether a match mode is currently active.</summary>
    public bool HasActiveMatchMode => ActiveMatchMode != "none" && !string.IsNullOrEmpty(ActiveMatchMode);

    /// <summary>Display text for the active mode banner (e.g., "Episode Mode — TheTVDB").</summary>
    public string ActiveMatchModeDisplay => ActiveMatchMode switch
    {
        "episode" => $"Episode Mode — {ActiveDatasourceLabel}",
        "movie"   => $"Movie Mode — {ActiveDatasourceLabel}",
        "music"   => $"Music Mode — {ActiveDatasourceLabel}",
        "smart"   => $"Smart Mode — {ActiveDatasourceLabel}",
        _         => string.Empty
    };

    /// <summary>Glyph icon for the active match mode.</summary>
    public string ActiveMatchModeGlyph => ActiveMatchMode switch
    {
        "episode" => "\uE786",  // TV
        "movie"   => "\uE8B2",  // Video
        "music"   => "\uE8D6",  // Music
        "smart"   => "\uE945",  // Processing
        _         => string.Empty
    };

    partial void OnActiveMatchModeChanged(string value)
    {
        OnPropertyChanged(nameof(HasActiveMatchMode));
        OnPropertyChanged(nameof(ActiveMatchModeDisplay));
        OnPropertyChanged(nameof(ActiveMatchModeGlyph));
    }

    partial void OnActiveDatasourceLabelChanged(string value)
    {
        OnPropertyChanged(nameof(ActiveMatchModeDisplay));
    }

    /// <summary>Saved presets loaded from settings.</summary>
    public ObservableCollection<PresetDefinitionSettings> Presets { get; } = [];

    /// <summary>Gets or sets the currently selected preset.</summary>
    [ObservableProperty]
    public partial PresetDefinitionSettings? SelectedPreset { get; set; }

    /// <summary>Gets whether a preset is currently active.</summary>
    public bool HasActivePreset => SelectedPreset is not null;

    /// <summary>Gets the display text for the Presets button (shows active preset name).</summary>
    public string PresetsButtonText => SelectedPreset is not null
        ? $"Preset: {SelectedPreset.Name}"
        : "Presets";
    /// <summary>Gets the number of files in the original files pane.</summary>
    public int FileCount => OriginalFiles.Count;

    /// <summary>Gets the number of files in the matched files pane.</summary>
    public int MatchedCount => MatchedFiles.Count;

    /// <summary>Gets a value indicating whether original files have been loaded.</summary>
    public bool HasFiles => OriginalFiles.Count > 0;

    /// <summary>Gets a value indicating whether the original files pane is empty.</summary>
    public bool HasNoFiles => OriginalFiles.Count == 0;

    /// <summary>Gets a value indicating whether matched files exist.</summary>
    public bool HasMatchedFiles => MatchedFiles.Count > 0;

    /// <summary>Gets a value indicating whether no matched files exist.</summary>
    public bool HasNoMatchedFiles => MatchedFiles.Count == 0;

    /// <summary>Gets a value indicating whether the empty-state placeholder should be shown.</summary>
    public bool ShowEmptyState => HasNoFiles && !IsScanning;

    /// <summary>Gets a formatted display string showing original and matched file counts.</summary>
    public string FileCountDisplay => $"{OriginalFiles.Count} file(s) | {MatchedFiles.Count} matched";

    /// <summary>Gets the available rename action labels for the UI combo box.</summary>
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
    public HomeViewModel() : this(null, null, null, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
    /// </summary>
    /// <param name="batchService">The batch rename service.</param>
    /// <param name="undoService">The undo/redo journal service.</param>
    /// <param name="matchingPipeline">The metadata matching pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="settingsRepository">The settings repository for loading presets.</param>
    public HomeViewModel(
        IBatchOperationService? batchService,
        IUndoService? undoService,
        IMatchingPipeline? matchingPipeline,
        ILogger<HomeViewModel>? logger,
        ISettingsRepository? settingsRepository = null)
    {
        _batchService = batchService;
        _undoService = undoService;
        _matchingPipeline = matchingPipeline;
        _settingsRepository = settingsRepository;
        _logger = logger ?? NullLogger<HomeViewModel>.Instance;

        OriginalFiles.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FileCount));
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(HasNoFiles));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(FileCountDisplay));
            UpdateStatusMessage();

            // Clear match mode indicator when all files are removed
            if (OriginalFiles.Count == 0)
            {
                ActiveMatchMode = "none";
                ActiveDatasourceLabel = string.Empty;
            }
        };

        MatchedFiles.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(MatchedCount));
            OnPropertyChanged(nameof(HasMatchedFiles));
            OnPropertyChanged(nameof(HasNoMatchedFiles));
            OnPropertyChanged(nameof(FileCountDisplay));
        };

        _ = RefreshCanUndoAsync();
        _ = LoadPresetsAsync();
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
    private async Task AddFilesAsync()
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files is null || files.Count == 0) return;

        AddFiles(files.Select(f => f.Path));
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
    private async Task MatchWithDatasourceAsync(string datasource)
    {
        // Set the active mode indicator regardless of files
        var (category, label) = CategorizeDatasource(datasource);
        ActiveMatchMode = category;
        ActiveDatasourceLabel = label;

        if (OriginalFiles.Count == 0)
        {
            StatusMessage = "No files loaded. Load a folder first, then match.";
            _notificationService?.ShowWarning("No files loaded. Drop files or click Load to get started.");
            return;
        }

        if (_matchingPipeline is null)
        {
            StatusMessage = "Matching pipeline not available.";
            return;
        }

        IsProcessing = true;
        StatusMessage = $"Matching via {datasource}...";
        MatchedFiles.Clear();

        try
        {
            var filePaths = OriginalFiles.Select(f => f.FilePath).ToList();
            var results = await _matchingPipeline.ProcessBatchAsync(filePaths, datasource);

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
            StatusMessage = $"Matched {matchCount}/{results.Count} file(s) via {datasource}.";
            _notificationService?.ShowSuccess($"Matched {matchCount} of {results.Count} file(s).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Matching with {Datasource} failed", datasource);
            StatusMessage = $"Match error: {ex.Message}";
            _notificationService?.ShowError($"Match error: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task EditFormatAsync()
    {
        try
        {
            var expressionEngine = App.GetService<Core.Expressions.IExpressionEngine>();
            var dialog = new ExpressionEditorDialog(expressionEngine) { XamlRoot = App.MainWindow.Content.XamlRoot };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open expression editor");
        }
    }

    [RelayCommand]
    private void OpenPreferences()
    {
        // Navigate to Settings page
        if (App.MainWindow.Content is Microsoft.UI.Xaml.Controls.Grid rootGrid)
        {
            var navView = rootGrid.Children.OfType<Microsoft.UI.Xaml.Controls.NavigationView>().FirstOrDefault();
            if (navView is not null)
            {
                navView.SelectedItem = navView.SettingsItem;
            }
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
    /// Handles both files and folders — folders are scanned recursively.
    /// </summary>
    public void AddFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            // If path is a directory, queue a scan
            if (Directory.Exists(path))
            {
                _ = ScanDroppedFolderAsync(path);
                continue;
            }

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

    /// <summary>
    /// Scans a dropped folder for media files, with progress feedback.
    /// </summary>
    public async Task ScanDroppedFolderAsync(string folderPath)
    {
        IsProcessing = true;
        IsScanning = true;
        SelectedFolder = folderPath;
        StatusMessage = $"Scanning {Path.GetFileName(folderPath)}...";

        try
        {
            await ScanFolderAsync(folderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan dropped folder {Folder}", folderPath);
            _notificationService?.ShowError($"Failed to scan folder: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            IsScanning = false;
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

    // ── Preset support ──────────────────────────────────────────────

    private static readonly string[] DatasourceValues = ["auto", "tmdb", "tvdb", "anidb", "musicbrainz"];
    private static readonly string[] MatchModeValues = ["opportunistic", "strict"];
    private static readonly RenameAction[] RenameActionValues =
        [RenameAction.Move, RenameAction.Copy, RenameAction.Hardlink, RenameAction.Symlink, RenameAction.Test];

    /// <summary>Loads presets from persisted settings into the Presets collection.</summary>
    public async Task LoadPresetsAsync()
    {
        if (_settingsRepository is null) return;

        try
        {
            var settings = await _settingsRepository.LoadAsync();
            Presets.Clear();
            foreach (var p in settings.Presets)
                Presets.Add(p);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load presets");
        }
    }

    /// <summary>Applies the selected preset's settings to the current session.</summary>
    partial void OnSelectedPresetChanged(PresetDefinitionSettings? value)
    {
        OnPropertyChanged(nameof(HasActivePreset));
        OnPropertyChanged(nameof(PresetsButtonText));
        if (value is null) return;
        ApplyPreset(value);
    }

    /// <summary>Maps a preset definition to the current session options.</summary>
    public void ApplyPreset(PresetDefinitionSettings preset)
    {
        _logger.LogInformation("Applying preset '{Name}'", preset.Name);

        // Match mode → SelectedModeIndex (0 = opportunistic, 1 = strict)
        var modeIdx = Array.IndexOf(MatchModeValues, preset.MatchMode);
        if (modeIdx >= 0) SelectedModeIndex = modeIdx;

        // Rename action → SelectedRenameActionIndex
        var actionIdx = Array.IndexOf(RenameActionValues, preset.RenameActionType);
        if (actionIdx >= 0) SelectedRenameActionIndex = actionIdx;

        // If the preset specifies an input folder, load it
        if (!string.IsNullOrWhiteSpace(preset.InputFolder) && Directory.Exists(preset.InputFolder))
        {
            SelectedFolder = preset.InputFolder;
            AddFiles(Directory.GetFiles(preset.InputFolder, "*.*", SearchOption.AllDirectories).ToList());
        }

        _notificationService?.ShowSuccess($"Preset \"{preset.Name}\" applied");
    }

    /// <summary>Opens the preset editor dialog to manage presets.</summary>
    [RelayCommand]
    private async Task EditPresetsAsync()
    {
        if (_settingsRepository is null) return;

        var settings = await _settingsRepository.LoadAsync();
        var presets = settings.Presets.ToList();

        var dialog = new PresetManagerDialog(presets)
        {
            XamlRoot = App.MainWindow.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            settings.Presets = dialog.Presets;
            await _settingsRepository.SaveAsync(settings);
            await LoadPresetsAsync();
            _notificationService?.ShowSuccess("Presets saved");
        }
    }

    /// <summary>
    /// Maps a datasource string to its mode category and display label.
    /// </summary>
    private static (string Category, string Label) CategorizeDatasource(string datasource) => datasource switch
    {
        "tvdb"     => ("episode", "TheTVDB"),
        "anidb"    => ("episode", "AniDB"),
        "tmdb_tv"  => ("episode", "TheMovieDB"),
        "tvmaze"   => ("episode", "TVmaze"),
        "tmdb"     => ("movie", "TheMovieDB"),
        "omdb"     => ("movie", "OMDb"),
        "acoustid" => ("music", "AcoustID"),
        "id3"      => ("music", "ID3 Tags"),
        "auto"     => ("smart", "Automatic"),
        "xattr"    => ("smart", "Attributes"),
        _          => ("smart", datasource)
    };
}
