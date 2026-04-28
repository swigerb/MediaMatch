using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.System;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for the About page — version info, links, and credits.
/// </summary>
public partial class AboutViewModel : ViewModelBase
{
    /// <summary>Gets the application name.</summary>
    public string AppName => "MediaMatch";

    /// <summary>Gets the formatted application version string.</summary>
    public string Version => $"v{GetType().Assembly.GetName().Version?.ToString(3) ?? "0.1.0"}";

    /// <summary>Gets the application description text.</summary>
    public string Description => "A modern, open-source media file organizer and renamer. " +
        "MediaMatch identifies TV episodes and movies, fetches metadata from online databases, " +
        "and renames your media files with a consistent, clean naming convention.";

    /// <summary>Gets the software license name.</summary>
    public string License => "MIT License";

    /// <summary>Gets the copyright notice.</summary>
    public string Copyright => $"© {DateTime.Now.Year} MediaMatch Contributors";

    /// <summary>Gets the GitHub repository URL.</summary>
    public string GitHubUrl => "https://github.com/swigerb/MediaMatch";

    [RelayCommand]
    private async Task OpenGitHubAsync()
    {
        await Launcher.LaunchUriAsync(new Uri(GitHubUrl));
    }
}
