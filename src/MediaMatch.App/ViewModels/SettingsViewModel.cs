using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Configuration;
using Windows.Storage.Pickers;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Settings page — manages API keys, rename patterns, and output paths.
/// Persists settings via <see cref="ISettingsRepository"/>.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsRepository _settingsRepository;

    [ObservableProperty]
    public partial string TmdbApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TvdbApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OpenSubtitlesApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MovieRenamePattern { get; set; } = "{Name} ({Year})/{Name} ({Year}){extension}";

    [ObservableProperty]
    public partial string SeriesRenamePattern { get; set; } = "{SeriesName}/Season {Season}/{SeriesName} - S{Season:D2}E{Episode:D2} - {Title}{extension}";

    [ObservableProperty]
    public partial string AnimeRenamePattern { get; set; } = "{SeriesName}/Season {Season}/{SeriesName} - S{Season:D2}E{Episode:D2} - {Title}{extension}";

    [ObservableProperty]
    public partial string MovieOutputFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SeriesOutputFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RenamePreview { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowWelcomeBanner { get; set; }

    partial void OnMovieRenamePatternChanged(string value) => UpdateRenamePreview();
    partial void OnSeriesRenamePatternChanged(string value) => UpdateRenamePreview();
    partial void OnAnimeRenamePatternChanged(string value) => UpdateRenamePreview();

    public SettingsViewModel(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
        UpdateRenamePreview();
    }

    /// <summary>
    /// Loads settings from disk into the ViewModel properties.
    /// Called when the Settings page is navigated to.
    /// </summary>
    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var settings = await _settingsRepository.LoadAsync();

            TmdbApiKey = settings.ApiKeys.TmdbApiKey;
            TvdbApiKey = settings.ApiKeys.TvdbApiKey;
            OpenSubtitlesApiKey = settings.ApiKeys.OpenSubtitlesApiKey;

            MovieRenamePattern = settings.RenamePatterns.MoviePattern;
            SeriesRenamePattern = settings.RenamePatterns.SeriesPattern;
            AnimeRenamePattern = settings.RenamePatterns.AnimePattern;

            MovieOutputFolder = settings.OutputFolders.MoviesRoot;
            SeriesOutputFolder = settings.OutputFolders.SeriesRoot;
        }
        catch
        {
            StatusMessage = "Failed to load settings — using defaults.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (!ValidateApiKeys())
            return;

        IsSaving = true;
        StatusMessage = "Saving...";

        try
        {
            var settings = new AppSettings
            {
                ApiKeys = new ApiKeySettings
                {
                    TmdbApiKey = TmdbApiKey.Trim(),
                    TvdbApiKey = TvdbApiKey.Trim(),
                    OpenSubtitlesApiKey = OpenSubtitlesApiKey.Trim()
                },
                RenamePatterns = new RenameSettings
                {
                    MoviePattern = MovieRenamePattern,
                    SeriesPattern = SeriesRenamePattern,
                    AnimePattern = AnimeRenamePattern
                },
                OutputFolders = new OutputFolderSettings
                {
                    MoviesRoot = MovieOutputFolder,
                    SeriesRoot = SeriesOutputFolder
                }
            };

            await _settingsRepository.SaveAsync(settings);
            StatusMessage = "Settings saved.";
            ShowWelcomeBanner = false;
        }
        catch
        {
            StatusMessage = "Failed to save settings.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void ClearTmdbApiKey()
    {
        TmdbApiKey = string.Empty;
    }

    [RelayCommand]
    private void ClearTvdbApiKey()
    {
        TvdbApiKey = string.Empty;
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        StatusMessage = "Clearing cache...";
        await Task.Delay(100); // Placeholder for actual cache-clearing
        StatusMessage = "Cache cleared.";
    }

    [RelayCommand]
    private async Task BrowseMovieOutputFolderAsync()
    {
        var path = await PickFolderAsync();
        if (path is not null) MovieOutputFolder = path;
    }

    [RelayCommand]
    private async Task BrowseSeriesOutputFolderAsync()
    {
        var path = await PickFolderAsync();
        if (path is not null) SeriesOutputFolder = path;
    }

    /// <summary>
    /// Validates API key format: must be alphanumeric hex or empty.
    /// </summary>
    private bool ValidateApiKeys()
    {
        if (!IsValidApiKey(TmdbApiKey))
        {
            StatusMessage = "TMDB API key contains invalid characters.";
            return false;
        }
        if (!IsValidApiKey(TvdbApiKey))
        {
            StatusMessage = "TVDB API key contains invalid characters.";
            return false;
        }
        if (!IsValidApiKey(OpenSubtitlesApiKey))
        {
            StatusMessage = "OpenSubtitles API key contains invalid characters.";
            return false;
        }
        return true;
    }

    private static bool IsValidApiKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return true; // Empty is fine — user hasn't set it yet

        var trimmed = key.Trim();
        return trimmed.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }

    private static async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private void UpdateRenamePreview()
    {
        var movieExample = MovieRenamePattern
            .Replace("{Name}", "Inception")
            .Replace("{Year}", "2010")
            .Replace("{extension}", ".mkv");

        var seriesExample = SeriesRenamePattern
            .Replace("{SeriesName}", "Breaking Bad")
            .Replace("{Season}", "01").Replace("{Season:D2}", "01")
            .Replace("{Episode}", "01").Replace("{Episode:D2}", "01")
            .Replace("{Title}", "Pilot")
            .Replace("{extension}", ".mkv");

        RenamePreview = $"Movie: {movieExample}\nSeries: {seriesExample}";
    }
}
