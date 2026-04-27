using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage.Pickers;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Home page — manages file list, folder selection, and rename operations.
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    public ObservableCollection<FileItemViewModel> Files { get; } = [];

    [ObservableProperty]
    public partial string SelectedFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "No files loaded. Add a folder to get started.";

    public int FileCount => Files.Count;
    public int SelectedCount => Files.Count(f => f.IsSelected);
    public bool HasFiles => Files.Count > 0;
    public bool HasNoFiles => Files.Count == 0;

    public HomeViewModel()
    {
        Files.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FileCount));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(HasNoFiles));
            UpdateStatusMessage();
        };
    }

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
        picker.FileTypeFilter.Add("*");

        // Get the current window's HWND for the picker
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
    private async Task ApplyRenamesAsync()
    {
        if (Files.Count == 0) return;

        IsProcessing = true;
        StatusMessage = "Applying renames...";

        try
        {
            // Rename logic will be wired to the Application layer service
            await Task.Delay(100); // Placeholder for actual rename operation
            StatusMessage = "Renames applied successfully.";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task ScanFolderAsync(string folderPath)
    {
        // Scan for media files — will be wired to Application layer
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
                    NewFileName = file.Name, // Placeholder until matching runs
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
}
