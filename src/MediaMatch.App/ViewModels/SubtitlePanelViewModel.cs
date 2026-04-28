using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Subtitles search/download panel.
/// </summary>
public partial class SubtitlePanelViewModel : ViewModelBase
{
    private readonly ISubtitleDownloadService? _subtitleService;
    private readonly ILogger<SubtitlePanelViewModel> _logger;

    /// <summary>Gets the collection of subtitle search results.</summary>
    public ObservableCollection<SubtitleResultViewModel> Results { get; } = [];

    /// <summary>Gets or sets the subtitle search query text.</summary>
    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected language filter index.</summary>
    [ObservableProperty]
    public partial int SelectedLanguageIndex { get; set; }

    /// <summary>Gets or sets a value indicating whether a search is in progress.</summary>
    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    /// <summary>Gets or sets the status message displayed in the panel.</summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the user needs to configure API credentials.</summary>
    [ObservableProperty]
    public partial bool NeedsLogin { get; set; }

    /// <summary>Gets the available language filter labels.</summary>
    public string[] LanguageOptions { get; } = ["All Languages", "English", "Japanese", "German", "French", "Spanish", "Portuguese", "Korean", "Chinese"];

    /// <summary>Gets a value indicating whether any search results exist.</summary>
    public bool HasResults => Results.Count > 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitlePanelViewModel"/> class for design-time use.
    /// </summary>
    public SubtitlePanelViewModel() : this(null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitlePanelViewModel"/> class.
    /// </summary>
    /// <param name="subtitleService">The subtitle download service.</param>
    /// <param name="logger">The logger instance.</param>
    public SubtitlePanelViewModel(ISubtitleDownloadService? subtitleService, ILogger<SubtitlePanelViewModel>? logger)
    {
        _subtitleService = subtitleService;
        _logger = logger ?? NullLogger<SubtitlePanelViewModel>.Instance;

        Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasResults));
    }

    [RelayCommand]
    private async Task FindAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        if (_subtitleService is null)
        {
            NeedsLogin = true;
            StatusMessage = "OpenSubtitles account required. Please configure your API key in Settings.";
            return;
        }

        IsSearching = true;
        Results.Clear();
        StatusMessage = $"Searching for \"{SearchQuery}\"...";

        try
        {
            var subtitles = await _subtitleService.SearchAsync(SearchQuery);
            foreach (var sub in subtitles)
            {
                Results.Add(new SubtitleResultViewModel
                {
                    Title = sub.FileName,
                    Language = sub.Language,
                    SubtitleCount = 1,
                    Descriptor = sub
                });
            }

            StatusMessage = $"{Results.Count} subtitle(s) found.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subtitle search failed for {Query}", SearchQuery);
            StatusMessage = $"Search error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task DownloadSelectedAsync()
    {
        if (_subtitleService is null) return;

        var selected = Results.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "No subtitles selected.";
            return;
        }

        StatusMessage = $"Downloading {selected.Count} subtitle(s)...";
        try
        {
            foreach (var sub in selected)
            {
                await _subtitleService.DownloadAsync(sub.Descriptor, sub.Descriptor.FileName);
            }
            StatusMessage = $"Downloaded {selected.Count} subtitle(s).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subtitle download failed");
            StatusMessage = $"Download error: {ex.Message}";
        }
    }
}

/// <summary>
/// Represents a subtitle search result in the panel.
/// </summary>
public partial class SubtitleResultViewModel : ViewModelBase
{
    /// <summary>Gets or sets the subtitle title or file name.</summary>
    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the subtitle language.</summary>
    [ObservableProperty]
    public partial string Language { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of subtitle entries in this result.</summary>
    [ObservableProperty]
    public partial int SubtitleCount { get; set; }

    /// <summary>Gets or sets a value indicating whether this result is selected for download.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Gets or sets the underlying subtitle descriptor from the provider.</summary>
    public SubtitleDescriptor Descriptor { get; set; } = null!;
}
