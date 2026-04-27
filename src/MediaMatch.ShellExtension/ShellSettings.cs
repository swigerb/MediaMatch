using System.Text.Json;

namespace MediaMatch.ShellExtension;

/// <summary>
/// Application settings for the shell extension, including preset definitions.
/// Loaded from appsettings.json next to the executable, or from %LOCALAPPDATA%/MediaMatch/shell-presets.json.
/// </summary>
public sealed class ShellSettings
{
    /// <summary>Path to the MediaMatch CLI executable.</summary>
    public string CliPath { get; set; } = "MediaMatch.CLI.exe";

    /// <summary>Custom presets that appear as sub-menu items in the context menu.</summary>
    public List<PresetDefinition> Presets { get; set; } = [];

    /// <summary>
    /// Loads settings from the user's local app data or falls back to defaults.
    /// </summary>
    public static ShellSettings Load()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaMatch", "shell-presets.json");

        if (File.Exists(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                return JsonSerializer.Deserialize<ShellSettings>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            catch
            {
                // Fall through to defaults
            }
        }

        return new ShellSettings();
    }
}
