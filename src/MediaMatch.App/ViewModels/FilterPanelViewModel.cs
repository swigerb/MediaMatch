using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Filter panel — media info inspection with tabbed detail view.
/// </summary>
public partial class FilterPanelViewModel : ViewModelBase
{
    private readonly IMediaAnalysisService? _analysisService;
    private readonly ILogger<FilterPanelViewModel> _logger;

    public ObservableCollection<FilterFileItemViewModel> Files { get; } = [];
    public ObservableCollection<MediaInfoEntry> MediaInfoEntries { get; } = [];

    [ObservableProperty]
    public partial FilterFileItemViewModel? SelectedFile { get; set; }

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public string[] TabOptions { get; } = ["Archives", "Types", "Parts", "Attributes", "MediaInfo"];

    public bool HasFiles => Files.Count > 0;

    public FilterPanelViewModel() : this(null, null) { }

    public FilterPanelViewModel(IMediaAnalysisService? analysisService, ILogger<FilterPanelViewModel>? logger)
    {
        _analysisService = analysisService;
        _logger = logger ?? NullLogger<FilterPanelViewModel>.Instance;

        Files.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFiles));
    }

    public void AddFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            var fi = new FileInfo(path);
            if (!fi.Exists) continue;

            Files.Add(new FilterFileItemViewModel
            {
                FileName = fi.Name,
                FilePath = fi.FullName,
                FolderName = fi.Directory?.Name ?? string.Empty
            });
        }
    }

    partial void OnSelectedFileChanged(FilterFileItemViewModel? value)
    {
        if (value is not null)
        {
            _ = LoadMediaInfoAsync(value.FilePath);
        }
    }

    [RelayCommand]
    private async Task LoadMediaInfoAsync(string filePath)
    {
        if (_analysisService is null)
        {
            StatusMessage = "Media analysis service not available.";
            return;
        }

        MediaInfoEntries.Clear();
        try
        {
            var result = await _analysisService.AnalyzeAsync(filePath);
            if (result is not null)
            {
                MediaInfoEntries.Add(new MediaInfoEntry("File", Path.GetFileName(filePath)));
                MediaInfoEntries.Add(new MediaInfoEntry("MediaType", result.MediaType.ToString()));
                MediaInfoEntries.Add(new MediaInfoEntry("Quality", result.Quality?.ToString() ?? "Unknown"));
                MediaInfoEntries.Add(new MediaInfoEntry("Year", result.Year?.ToString() ?? "N/A"));
                MediaInfoEntries.Add(new MediaInfoEntry("ReleaseGroup", result.ReleaseGroup ?? "N/A"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze {File}", filePath);
            StatusMessage = $"Analysis error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Files.Clear();
        MediaInfoEntries.Clear();
        SelectedFile = null;
    }
}

/// <summary>
/// A file item in the Filter panel's tree view.
/// </summary>
public partial class FilterFileItemViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FolderName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>
/// A key-value pair for media info display.
/// </summary>
public sealed record MediaInfoEntry(string Key, string Value);
