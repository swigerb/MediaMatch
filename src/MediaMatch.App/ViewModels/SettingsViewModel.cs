using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage.Pickers;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the Settings page — manages API keys, rename patterns, and output paths.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string TmdbApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TvdbApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MovieRenamePattern { get; set; } = "{Name} ({Year})/{Name} ({Year}){extension}";

    [ObservableProperty]
    public partial string SeriesRenamePattern { get; set; } = "{SeriesName}/Season {Season}/{SeriesName} - S{Season:D2}E{Episode:D2} - {Title}{extension}";

    [ObservableProperty]
    public partial string MovieOutputFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SeriesOutputFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RenamePreview { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    partial void OnMovieRenamePatternChanged(string value) => UpdateRenamePreview();
    partial void OnSeriesRenamePatternChanged(string value) => UpdateRenamePreview();

    public SettingsViewModel()
    {
        UpdateRenamePreview();
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        IsSaving = true;
        StatusMessage = "Saving...";

        try
        {
            // Settings persistence will be wired to Application layer
            await Task.Delay(100); // Placeholder
            StatusMessage = "Settings saved.";
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
