using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Subtitles search/download panel.
/// </summary>
public partial class SubtitlePanelViewModel : ViewModelBase
{
    private readonly ISubtitleProvider? _subtitleProvider;
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
    /// <param name="subtitleProvider">The subtitle provider for search and download.</param>
    /// <param name="logger">The logger instance.</param>
    public SubtitlePanelViewModel(ISubtitleProvider? subtitleProvider, ILogger<SubtitlePanelViewModel>? logger)
    {
        _subtitleProvider = subtitleProvider;
        _logger = logger ?? NullLogger<SubtitlePanelViewModel>.Instance;

        Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasResults));
    }

    [RelayCommand]
    private async Task FindAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        if (_subtitleProvider is null)
        {
            NeedsLogin = true;
            StatusMessage = "OpenSubtitles account required. Please configure your API key in Settings.";
            return;
        }

        IsSearching = true;
        Results.Clear();
        var language = SelectedLanguageIndex > 0 ? LanguageOptions[SelectedLanguageIndex].ToLowerInvariant() : "all";
        StatusMessage = $"Searching for \"{SearchQuery}\"...";

        try
        {
            var subtitles = await _subtitleProvider.SearchAsync(SearchQuery, language);
            foreach (var sub in subtitles)
            {
                Results.Add(new SubtitleResultViewModel
                {
                    Title = sub.Name,
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
        if (_subtitleProvider is null) return;

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
                using var stream = await _subtitleProvider.DownloadAsync(sub.Descriptor);
                var filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"{sub.Descriptor.Name}.{sub.Descriptor.Format.ToString().ToLowerInvariant()}");
                await using var fileStream = File.Create(filePath);
                await stream.CopyToAsync(fileStream);
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
