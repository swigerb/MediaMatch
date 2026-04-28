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

    /// <summary>Gets the collection of episodes returned by search.</summary>
    public ObservableCollection<Episode> Episodes { get; } = [];

    /// <summary>Gets or sets the series search query text.</summary>
    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected data source index.</summary>
    [ObservableProperty]
    public partial int SelectedDatasourceIndex { get; set; }

    /// <summary>Gets or sets the selected season filter index.</summary>
    [ObservableProperty]
    public partial int SelectedSeasonIndex { get; set; }

    /// <summary>Gets or sets the selected episode ordering index.</summary>
    [ObservableProperty]
    public partial int SelectedOrderIndex { get; set; }

    /// <summary>Gets or sets the selected language index.</summary>
    [ObservableProperty]
    public partial int SelectedLanguageIndex { get; set; }

    /// <summary>Gets or sets a value indicating whether a search is in progress.</summary>
    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    /// <summary>Gets or sets the status message displayed in the panel.</summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets the breadcrumb navigation text.</summary>
    [ObservableProperty]
    public partial string BreadcrumbText { get; set; } = string.Empty;

    /// <summary>Gets the available data source labels.</summary>
    public string[] DatasourceOptions { get; } = ["TheTVDB", "AniDB", "TheMovieDB", "TVmaze"];

    /// <summary>Gets the available season filter labels.</summary>
    public string[] SeasonOptions { get; } = ["All Seasons", "Season 1", "Season 2", "Season 3", "Specials"];

    /// <summary>Gets the available episode ordering labels.</summary>
    public string[] OrderOptions { get; } = ["Airdate", "DVD", "Absolute"];

    /// <summary>Gets the available language labels.</summary>
    public string[] LanguageOptions { get; } = ["English", "Japanese", "German", "French", "Spanish"];

    /// <summary>Gets a value indicating whether any episodes have been loaded.</summary>
    public bool HasEpisodes => Episodes.Count > 0;

    /// <summary>Gets a value indicating whether no episodes are loaded and no search is running.</summary>
    public bool HasNoEpisodes => Episodes.Count == 0 && !IsSearching;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodesPanelViewModel"/> class for design-time use.
    /// </summary>
    public EpisodesPanelViewModel() : this(null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodesPanelViewModel"/> class.
    /// </summary>
    /// <param name="episodeProvider">The episode metadata provider.</param>
    /// <param name="logger">The logger instance.</param>
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

            var episodes = await _episodeProvider.GetEpisodesAsync(first);
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
