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

    /// <summary>Gets the collection of files loaded for analysis.</summary>
    public ObservableCollection<FilterFileItemViewModel> Files { get; } = [];

    /// <summary>Gets the collection of media info key-value entries for the selected file.</summary>
    public ObservableCollection<MediaInfoEntry> MediaInfoEntries { get; } = [];

    /// <summary>Gets or sets the currently selected file in the panel.</summary>
    [ObservableProperty]
    public partial FilterFileItemViewModel? SelectedFile { get; set; }

    /// <summary>Gets or sets the selected detail tab index.</summary>
    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    /// <summary>Gets or sets the status message displayed in the panel.</summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Gets the available tab labels for the detail view.</summary>
    public string[] TabOptions { get; } = ["Archives", "Types", "Parts", "Attributes", "MediaInfo"];

    /// <summary>Gets a value indicating whether any files are loaded.</summary>
    public bool HasFiles => Files.Count > 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterPanelViewModel"/> class for design-time use.
    /// </summary>
    public FilterPanelViewModel() : this(null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterPanelViewModel"/> class.
    /// </summary>
    /// <param name="analysisService">The media analysis service.</param>
    /// <param name="logger">The logger instance.</param>
    public FilterPanelViewModel(IMediaAnalysisService? analysisService, ILogger<FilterPanelViewModel>? logger)
    {
        _analysisService = analysisService;
        _logger = logger ?? NullLogger<FilterPanelViewModel>.Instance;

        Files.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFiles));
    }

    /// <summary>
    /// Adds files from the specified paths to the panel.
    /// </summary>
    /// <param name="filePaths">The file paths to add.</param>
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
                MediaInfoEntries.Add(new MediaInfoEntry("Quality", result.VideoQuality ?? "Unknown"));
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
    /// <summary>Gets or sets the file name.</summary>
    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the full file path.</summary>
    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the parent folder name.</summary>
    [ObservableProperty]
    public partial string FolderName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this item is selected.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>
/// A key-value pair for media info display.
/// </summary>
/// <param name="Key">The property key.</param>
/// <param name="Value">The property value.</param>
public sealed record MediaInfoEntry(string Key, string Value);
