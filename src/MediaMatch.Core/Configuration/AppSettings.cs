namespace MediaMatch.Core.Configuration;

/// <summary>
/// Root application settings shared between the App and CLI.
/// Serialized to settings.json in %LOCALAPPDATA%/MediaMatch.
/// </summary>
public sealed class AppSettings
{
    /// <summary>API key credentials for metadata providers.</summary>
    public ApiKeySettings ApiKeys { get; set; } = new();

    /// <summary>Default rename patterns per media type.</summary>
    public RenameSettings RenamePatterns { get; set; } = new();

    /// <summary>Metadata cache duration in minutes.</summary>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>Root output folders for organized media.</summary>
    public OutputFolderSettings OutputFolders { get; set; } = new();
}

/// <summary>
/// API key values for external metadata services.
/// Values are encrypted at rest via <see cref="ISettingsEncryption"/>.
/// </summary>
public sealed class ApiKeySettings
{
    public string TmdbApiKey { get; set; } = string.Empty;
    public string TvdbApiKey { get; set; } = string.Empty;
    public string OpenSubtitlesApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Default rename patterns for each media type.
/// Tokens like {Name}, {Year}, {SeriesName}, {Season}, {Episode}, {Title} are replaced at rename time.
/// </summary>
public sealed class RenameSettings
{
    public string MoviePattern { get; set; } = "{Name} ({Year})/{Name} ({Year}){extension}";
    public string SeriesPattern { get; set; } = "{SeriesName}/Season {Season}/{SeriesName} - S{Season:D2}E{Episode:D2} - {Title}{extension}";
    public string AnimePattern { get; set; } = "{SeriesName}/Season {Season}/{SeriesName} - S{Season:D2}E{Episode:D2} - {Title}{extension}";
}

/// <summary>
/// Root output folder paths for organized media.
/// </summary>
public sealed class OutputFolderSettings
{
    public string MoviesRoot { get; set; } = string.Empty;
    public string SeriesRoot { get; set; } = string.Empty;
}
