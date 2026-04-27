namespace MediaMatch.ShellExtension;

/// <summary>
/// Defines a custom preset that appears as a sub-menu item under "MediaMatch" in the Windows context menu.
/// </summary>
public sealed class PresetDefinition
{
    /// <summary>Display name in the context menu (e.g., "TV Shows → Plex").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Rename pattern to pass to the CLI (e.g., "{SeriesName}/Season {Season}/...").</summary>
    public string RenamePattern { get; set; } = string.Empty;

    /// <summary>Output folder for organized files (e.g., "D:\TV").</summary>
    public string OutputFolder { get; set; } = string.Empty;

    /// <summary>Optional post-rename actions (e.g., "notify", "cleanup-empty-dirs").</summary>
    public List<string> PostActions { get; set; } = [];
}
