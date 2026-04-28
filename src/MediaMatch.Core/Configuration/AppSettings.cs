using MediaMatch.Core.Enums;

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

    /// <summary>Prefer local metadata (NFO/XML) over online providers.</summary>
    public bool PreferLocalMetadata { get; set; } = true;

    /// <summary>Default rename pattern for music files.</summary>
    public string MusicRenamePattern { get; set; } = "{albumartist}/{album}/{disc:D1}-{track:D2} {title}{extension}";

    /// <summary>Post-processing actions to run after successful renames.</summary>
    public List<PostProcessActionSettings> PostProcessActions { get; set; } = [];

    /// <summary>Plex media server settings for post-processing.</summary>
    public PlexSettings Plex { get; set; } = new();

    /// <summary>Jellyfin media server settings for post-processing.</summary>
    public JellyfinSettings Jellyfin { get; set; } = new();

    /// <summary>Performance tuning for file scanning and metadata resolution.</summary>
    public PerformanceSettings Performance { get; set; } = new();
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

    /// <summary>Metadata datasource: auto, tmdb, tvdb, anidb, musicbrainz.</summary>
    public string Datasource { get; set; } = "auto";

    /// <summary>ISO language code for metadata lookups.</summary>
    public string Language { get; set; } = "en";

    /// <summary>Episode ordering: airdate, dvd, absolute.</summary>
    public string EpisodeOrder { get; set; } = "airdate";

    /// <summary>Match mode: opportunistic or strict.</summary>
    public string MatchMode { get; set; } = "opportunistic";

    /// <summary>Rename action to perform (Move, Copy, etc.).</summary>
    public RenameAction RenameActionType { get; set; } = RenameAction.Move;

    /// <summary>Source input folder path.</summary>
    public string InputFolder { get; set; } = string.Empty;

    /// <summary>Glob pattern to filter input files (e.g., "*.mkv, *.mp4").</summary>
    public string IncludeFilter { get; set; } = string.Empty;

    /// <summary>Keyboard shortcut (e.g., "Ctrl+1").</summary>
    public string KeyboardShortcut { get; set; } = string.Empty;
}

/// <summary>
/// API key values for external metadata services.
/// Values are encrypted at rest via <see cref="ISettingsEncryption"/>.
/// </summary>
public sealed class ApiKeySettings
{
    /// <summary>Gets or sets the TMDb API key.</summary>
    public string TmdbApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the TVDb API key.</summary>
    public string TvdbApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the OpenSubtitles API key.</summary>
    public string OpenSubtitlesApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the AcoustID API key.</summary>
    public string AcoustIdApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Default rename patterns for each media type.
/// Tokens like {Name}, {Year}, {SeriesName}, {Season}, {Episode}, {Title} are replaced at rename time.
/// </summary>
public sealed class RenameSettings
{
    /// <summary>Gets or sets the rename pattern for movie files.</summary>
    public string MoviePattern { get; set; } = "{Name} ({Year})/{Name} ({Year}){extension}";

    /// <summary>Gets or sets the rename pattern for TV series episodes.</summary>
    public string SeriesPattern { get; set; } = "{SeriesName}/Season {Season}/{SeriesName} - S{Season:D2}E{Episode:D2} - {Title}{extension}";

    /// <summary>Gets or sets the rename pattern for anime episodes.</summary>
    public string AnimePattern { get; set; } = "{SeriesName}/Season {Season}/{SeriesName} - S{Season:D2}E{Episode:D2} - {Title}{extension}";
}

/// <summary>
/// Root output folder paths for organized media.
/// </summary>
public sealed class OutputFolderSettings
{
    /// <summary>Gets or sets the root output folder for organized movie files.</summary>
    public string MoviesRoot { get; set; } = string.Empty;

    /// <summary>Gets or sets the root output folder for organized TV series files.</summary>
    public string SeriesRoot { get; set; } = string.Empty;
}

/// <summary>Configuration for a post-processing action.</summary>
public sealed class PostProcessActionSettings
{
    /// <summary>Action name (e.g., "plex-refresh", "thumbnail", "custom-script").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this action is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Optional configuration value (e.g., script path for custom-script).</summary>
    public string? Config { get; set; }
}

/// <summary>Plex media server connection settings.</summary>
public sealed class PlexSettings
{
    /// <summary>Gets or sets the Plex server URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the Plex authentication token.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Gets or sets the library section IDs to refresh after rename.</summary>
    public List<string> LibrarySectionIds { get; set; } = [];
}

/// <summary>Jellyfin media server connection settings.</summary>
public sealed class JellyfinSettings
{
    /// <summary>Gets or sets the Jellyfin server URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the Jellyfin API key.</summary>
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>App theme modes.</summary>
public enum ThemeMode
{
    /// <summary>Follows the operating system theme.</summary>
    System = 0,

    /// <summary>Light theme.</summary>
    Light = 1,

    /// <summary>Dark theme.</summary>
    Dark = 2
}

/// <summary>Accessibility font scale levels.</summary>
public enum FontScale
{
    /// <summary>Smaller than default font size.</summary>
    Small = 0,

    /// <summary>Default font size.</summary>
    Medium = 1,

    /// <summary>Larger than default font size.</summary>
    Large = 2,

    /// <summary>Maximum font size for accessibility.</summary>
    ExtraLarge = 3
}
