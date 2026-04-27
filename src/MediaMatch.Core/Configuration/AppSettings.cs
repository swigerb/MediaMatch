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

    /// <summary>Theme mode preference: System (default), Light, or Dark.</summary>
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    /// <summary>Font scale for accessibility: Small, Medium (default), Large, ExtraLarge.</summary>
    public FontScale FontScale { get; set; } = FontScale.Medium;

    /// <summary>
    /// Enable opportunistic matching when strict matching (≥0.85) fails.
    /// Falls back to a 0.60 threshold and returns ranked suggestions.
    /// </summary>
    public bool EnableOpportunisticMode { get; set; } = true;

    /// <summary>LLM provider settings for AI-assisted renaming.</summary>
    public LlmConfiguration LlmSettings { get; set; } = new();

    /// <summary>Multi-episode naming strategy.</summary>
    public MultiEpisodeNamingStrategy MultiEpisodeNaming { get; set; } = MultiEpisodeNamingStrategy.Plex;

    /// <summary>
    /// Custom preset definitions for quick-action context menu and batch operations.
    /// </summary>
    public List<PresetDefinitionSettings> Presets { get; set; } = [];
}

/// <summary>
/// Multi-episode naming conventions for different media servers.
/// </summary>
public enum MultiEpisodeNamingStrategy
{
    /// <summary>Plex style: Show - S01E01-E02 - Title1 &amp; Title2.mkv</summary>
    Plex = 0,

    /// <summary>Jellyfin style: Show S01E01-S01E02.mkv</summary>
    Jellyfin = 1,

    /// <summary>Custom pattern using {startEpisode} and {endEpisode} tokens.</summary>
    Custom = 2
}

/// <summary>
/// A named preset for quick rename/organize operations.
/// Used by both the Shell Extension context menu and the App UI.
/// </summary>
public sealed class PresetDefinitionSettings
{
    /// <summary>Display name (e.g., "TV Shows → Plex").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Rename pattern tokens (e.g., "{SeriesName}/Season {Season}/...").</summary>
    public string RenamePattern { get; set; } = string.Empty;

    /// <summary>Target output folder.</summary>
    public string OutputFolder { get; set; } = string.Empty;

    /// <summary>Optional post-rename actions.</summary>
    public List<string> PostActions { get; set; } = [];
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

/// <summary>App theme modes.</summary>
public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}

/// <summary>Accessibility font scale levels.</summary>
public enum FontScale
{
    Small = 0,
    Medium = 1,
    Large = 2,
    ExtraLarge = 3
}
