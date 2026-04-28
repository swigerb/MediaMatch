using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Configuration;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Settings page — manages API keys, rename patterns, output paths,
/// theme mode, and font scale settings.
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

    /// <summary>Selected theme mode index: 0=System, 1=Light, 2=Dark.</summary>
    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; }

    /// <summary>Selected font scale index: 0=Small, 1=Medium, 2=Large, 3=ExtraLarge.</summary>
    [ObservableProperty]
    public partial int SelectedFontScaleIndex { get; set; } = 1;

    /// <summary>Font scale label descriptions for the UI.</summary>
    public string[] ThemeOptions { get; } = ["System", "Light", "Dark"];

    /// <summary>Font scale label descriptions for the UI.</summary>
    public string[] FontScaleOptions { get; } = ["Small (12px)", "Medium (14px)", "Large (16px)", "Extra Large (18px)"];

    /// <summary>Preview text showing current font scale.</summary>
    public string FontPreviewText => "The quick brown fox jumps over the lazy dog. MediaMatch renames your media files automatically.";

    /// <summary>Font size in pixels for the preview TextBlock.</summary>
    public double FontPreviewSize => SelectedFontScaleIndex switch
    {
        0 => 12.0,
        2 => 16.0,
        3 => 18.0,
        _ => 14.0
    };

    /// <summary>Caption describing the current font scale selection.</summary>
    public string FontPreviewCaption => SelectedFontScaleIndex switch
    {
        0 => "Small — 12px base size",
        1 => "Medium — 14px base size (default)",
        2 => "Large — 16px base size",
        3 => "Extra Large — 18px base size",
        _ => "Medium — 14px base size"
    };

    partial void OnMovieRenamePatternChanged(string value) => UpdateRenamePreview();
    partial void OnSeriesRenamePatternChanged(string value) => UpdateRenamePreview();
    partial void OnAnimeRenamePatternChanged(string value) => UpdateRenamePreview();

    partial void OnSelectedThemeIndexChanged(int value) => ApplyTheme((ThemeMode)value);
    partial void OnSelectedFontScaleIndexChanged(int value)
    {
        ApplyFontScale((FontScale)value);
        OnPropertyChanged(nameof(FontPreviewCaption));
        OnPropertyChanged(nameof(FontPreviewSize));
    }

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

            SelectedThemeIndex = (int)settings.ThemeMode;
            SelectedFontScaleIndex = (int)settings.FontScale;
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
                },
                ThemeMode = (ThemeMode)SelectedThemeIndex,
                FontScale = (FontScale)SelectedFontScaleIndex
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

    /// <summary>
    /// Applies the theme to the root element and updates title bar colors.
    /// </summary>
    public static void ApplyTheme(ThemeMode themeMode)
    {
        if (App.MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = themeMode switch
            {
                ThemeMode.Light => ElementTheme.Light,
                ThemeMode.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            UpdateTitleBarColors(rootElement.ActualTheme);
        }
    }

    /// <summary>
    /// Applies the font scale globally by setting FontSize on the NavigationView,
    /// which cascades to all child controls via WinUI font inheritance.
    /// </summary>
    public static void ApplyFontScale(FontScale fontScale)
    {
        if (App.MainWindow?.Content is FrameworkElement rootElement)
        {
            var baseFontSize = fontScale switch
            {
                FontScale.Small => 12.0,
                FontScale.Large => 16.0,
                FontScale.ExtraLarge => 18.0,
                _ => 14.0
            };

            // The root content is a Grid; find the NavigationView inside it
            // which is a Control and supports FontSize inheritance.
            if (rootElement is Microsoft.UI.Xaml.Controls.Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Microsoft.UI.Xaml.Controls.Control control)
                    {
                        control.FontSize = baseFontSize;
                    }
                }
            }
            else if (rootElement is Microsoft.UI.Xaml.Controls.Control rootControl)
            {
                rootControl.FontSize = baseFontSize;
            }
        }
    }

    /// <summary>
    /// Updates title bar button colors to match the current theme.
    /// </summary>
    internal static void UpdateTitleBarColors(ElementTheme actualTheme)
    {
        var titleBar = App.MainWindow?.AppWindow?.TitleBar;
        if (titleBar is null) return;

        if (actualTheme == ElementTheme.Dark)
        {
            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF);
            titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF);
        }
        else
        {
            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
            titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(0x33, 0x00, 0x00, 0x00);
            titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(0x66, 0x00, 0x00, 0x00);
        }

        // Keep background transparent for Mica backdrop
        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
    }
}
