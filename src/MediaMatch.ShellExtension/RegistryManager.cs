using Microsoft.Win32;

namespace MediaMatch.ShellExtension;

/// <summary>
/// Manages Windows Registry entries for the context menu integration.
/// Uses HKCU\Software\Classes\*\shell\MediaMatch registry key approach for per-user install.
/// </summary>
public static class RegistryManager
{
    private const string BaseKey = @"Software\Classes\*\shell\MediaMatch";

    /// <summary>
    /// Installs context menu entries for MediaMatch in the Windows Explorer context menu.
    /// </summary>
    public static void Install(string cliPath, IReadOnlyList<PresetDefinition> presets)
    {
        var exePath = GetShellExtensionPath();

        // Main menu entry
        using (var key = Registry.CurrentUser.CreateSubKey(BaseKey))
        {
            key.SetValue("", "MediaMatch");
            key.SetValue("Icon", exePath);
            key.SetValue("SubCommands", "");
        }

        // Sub-command: Rename with MediaMatch
        CreateSubCommand("Rename", "Rename with MediaMatch",
            $"\"{exePath}\" rename \"%1\"");

        // Sub-command: Match & Preview
        CreateSubCommand("Match", "Match && Preview",
            $"\"{exePath}\" match \"%1\"");

        // Sub-command: Organize to Library
        CreateSubCommand("Organize", "Organize to Library",
            $"\"{exePath}\" organize \"%1\"");

        // Custom preset sub-commands
        for (int i = 0; i < presets.Count; i++)
        {
            var preset = presets[i];
            var safeName = SanitizeKeyName(preset.Name);
            CreateSubCommand($"Preset_{safeName}",
                preset.Name,
                $"\"{exePath}\" preset --name \"{preset.Name}\" \"%1\"");
        }

        Console.WriteLine("MediaMatch context menu installed successfully.");
        Console.WriteLine($"CLI path: {cliPath}");
        Console.WriteLine($"Presets registered: {presets.Count}");
    }

    /// <summary>
    /// Removes all MediaMatch context menu entries from the registry.
    /// </summary>
    public static void Uninstall()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(BaseKey, throwOnMissingSubKey: false);
            Console.WriteLine("MediaMatch context menu removed successfully.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to remove context menu: {ex.Message}");
        }
    }

    private static void CreateSubCommand(string id, string label, string command)
    {
        var shellKey = $@"{BaseKey}\shell\{id}";

        using (var key = Registry.CurrentUser.CreateSubKey(shellKey))
        {
            key.SetValue("", label);
        }

        using (var cmdKey = Registry.CurrentUser.CreateSubKey($@"{shellKey}\command"))
        {
            cmdKey.SetValue("", command);
        }
    }

    private static string GetShellExtensionPath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "MediaMatch.ShellExtension.exe");
    }

    private static string SanitizeKeyName(string name)
    {
        // Remove characters not valid in registry key names
        var sanitized = new string(name
            .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
            .ToArray());
        return string.IsNullOrEmpty(sanitized) ? "preset" : sanitized;
    }
}
