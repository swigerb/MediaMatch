using MediaMatch.Core.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Linux.Pages;

/// <summary>
/// Settings page for API keys, theme, and cache management.
/// </summary>
public sealed partial class SettingsPage : Page
{
    private readonly ISettingsRepository _settingsRepo;

    public SettingsPage()
    {
        InitializeComponent();
        _settingsRepo = App.GetService<ISettingsRepository>();
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsRepo.LoadAsync();
            TmdbKeyBox.Text = settings.ApiKeys.TmdbApiKey ?? string.Empty;
            TvdbKeyBox.Text = settings.ApiKeys.TvdbApiKey ?? string.Empty;
            OpenSubsKeyBox.Text = settings.ApiKeys.OpenSubtitlesApiKey ?? string.Empty;
            ThemeComboBox.SelectedIndex = (int)settings.ThemeMode;
        }
        catch
        {
            ShowNotification("Failed to load settings.", InfoBarSeverity.Error);
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = await _settingsRepo.LoadAsync();
            settings.ApiKeys.TmdbApiKey = TmdbKeyBox.Text;
            settings.ApiKeys.TvdbApiKey = TvdbKeyBox.Text;
            settings.ApiKeys.OpenSubtitlesApiKey = OpenSubsKeyBox.Text;
            settings.ThemeMode = (Core.Configuration.ThemeMode)ThemeComboBox.SelectedIndex;

            await _settingsRepo.SaveAsync(settings);
            ShowNotification("Settings saved.", InfoBarSeverity.Success);
        }
        catch
        {
            ShowNotification("Failed to save settings.", InfoBarSeverity.Error);
        }
    }

    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var cache = App.GetService<IMemoryCache>();
            if (cache is MemoryCache mc)
                mc.Compact(1.0);

            ShowNotification("Cache cleared.", InfoBarSeverity.Success);
        }
        catch
        {
            ShowNotification("Failed to clear cache.", InfoBarSeverity.Warning);
        }
    }

    private void ShowNotification(string message, InfoBarSeverity severity)
    {
        SettingsNotification.Message = message;
        SettingsNotification.Severity = severity;
        SettingsNotification.IsOpen = true;
    }
}
