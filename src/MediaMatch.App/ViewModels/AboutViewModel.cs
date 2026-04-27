using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.System;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the About page — version info, links, and credits.
/// </summary>
public partial class AboutViewModel : ViewModelBase
{
    public string AppName => "MediaMatch";

    public string Version => $"v{GetType().Assembly.GetName().Version?.ToString(3) ?? "0.1.0"}";

    public string Description => "A modern, open-source media file organizer and renamer. " +
        "MediaMatch identifies TV episodes and movies, fetches metadata from online databases, " +
        "and renames your media files with a consistent, clean naming convention.";

    public string License => "MIT License";

    public string Copyright => $"© {DateTime.Now.Year} MediaMatch Contributors";

    public string GitHubUrl => "https://github.com/swigerb/MediaMatch";

    [RelayCommand]
    private async Task OpenGitHubAsync()
    {
        await Launcher.LaunchUriAsync(new Uri(GitHubUrl));
    }
}
