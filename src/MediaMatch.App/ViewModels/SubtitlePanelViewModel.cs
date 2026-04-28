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

    public ObservableCollection<SubtitleResultViewModel> Results { get; } = [];

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedLanguageIndex { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NeedsLogin { get; set; }

    public string[] LanguageOptions { get; } = ["All Languages", "English", "Japanese", "German", "French", "Spanish", "Portuguese", "Korean", "Chinese"];

    public bool HasResults => Results.Count > 0;

    public SubtitlePanelViewModel() : this(null, null) { }

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
    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Language { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SubtitleCount { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public SubtitleDescriptor Descriptor { get; set; } = null!;
}
