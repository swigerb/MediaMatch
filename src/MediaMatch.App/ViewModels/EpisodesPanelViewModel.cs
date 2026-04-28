using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Episodes browser panel — search a series and list all episodes.
/// </summary>
public partial class EpisodesPanelViewModel : ViewModelBase
{
    private readonly IEpisodeProvider? _episodeProvider;
    private readonly ILogger<EpisodesPanelViewModel> _logger;

    public ObservableCollection<Episode> Episodes { get; } = [];

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedDatasourceIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedSeasonIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedOrderIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedLanguageIndex { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BreadcrumbText { get; set; } = string.Empty;

    public string[] DatasourceOptions { get; } = ["TheTVDB", "AniDB", "TheMovieDB", "TVmaze"];
    public string[] SeasonOptions { get; } = ["All Seasons", "Season 1", "Season 2", "Season 3", "Specials"];
    public string[] OrderOptions { get; } = ["Airdate", "DVD", "Absolute"];
    public string[] LanguageOptions { get; } = ["English", "Japanese", "German", "French", "Spanish"];

    public bool HasEpisodes => Episodes.Count > 0;
    public bool HasNoEpisodes => Episodes.Count == 0 && !IsSearching;

    public EpisodesPanelViewModel() : this(null, null) { }

    public EpisodesPanelViewModel(IEpisodeProvider? episodeProvider, ILogger<EpisodesPanelViewModel>? logger)
    {
        _episodeProvider = episodeProvider;
        _logger = logger ?? NullLogger<EpisodesPanelViewModel>.Instance;

        Episodes.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasEpisodes));
            OnPropertyChanged(nameof(HasNoEpisodes));
        };
    }

    [RelayCommand]
    private async Task FindAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        if (_episodeProvider is null)
        {
            StatusMessage = "Episode provider not available.";
            return;
        }

        IsSearching = true;
        Episodes.Clear();
        StatusMessage = $"Searching for \"{SearchQuery}\"...";

        try
        {
            var results = await _episodeProvider.SearchAsync(SearchQuery);
            if (results.Count == 0)
            {
                StatusMessage = "No results found.";
                return;
            }

            // Use the first result to fetch episodes
            var first = results[0];
            BreadcrumbText = $"Search Results > {first.Name}";

            var episodes = await _episodeProvider.GetEpisodesAsync(first.Id);
            foreach (var ep in episodes)
            {
                Episodes.Add(ep);
            }

            StatusMessage = $"{Episodes.Count} episode(s) found.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Episode search failed for {Query}", SearchQuery);
            StatusMessage = $"Search error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }
}
