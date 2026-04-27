using System.Diagnostics;

namespace MediaMatch.ShellExtension;

/// <summary>
/// Entry point for the MediaMatch shell extension.
/// Handles install/uninstall registration and context menu action dispatching.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLowerInvariant();
        var settings = ShellSettings.Load();

        return command switch
        {
            "install" => HandleInstall(settings),
            "uninstall" => HandleUninstall(),
            "rename" => HandleCliCommand("rename", args[1..], settings),
            "match" => HandleCliCommand("match", args[1..], settings),
            "organize" => HandleCliCommand("rename", args[1..], settings),
            "preset" => HandlePreset(args[1..], settings),
            _ => HandleUnknown(command)
        };
    }

    private static int HandleInstall(ShellSettings settings)
    {
        try
        {
            RegistryManager.Install(settings.CliPath, settings.Presets);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Install failed: {ex.Message}");
            return 1;
        }
    }

    private static int HandleUninstall()
    {
        try
        {
            RegistryManager.Uninstall();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Uninstall failed: {ex.Message}");
            return 1;
        }
    }

    private static int HandleCliCommand(string cliCommand, string[] filePaths, ShellSettings settings)
    {
        if (filePaths.Length == 0)
        {
            Console.Error.WriteLine("No files specified.");
            return 1;
        }

        var cliPath = ResolveCliPath(settings.CliPath);
        var quotedFiles = string.Join(" ", filePaths.Select(f => $"\"{f}\""));
        var arguments = $"{cliCommand} --files {quotedFiles}";

        return LaunchCli(cliPath, arguments);
    }

    private static int HandlePreset(string[] args, ShellSettings settings)
    {
        // Parse --name "PresetName" and file paths
        string? presetName = null;
        var files = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--name" && i + 1 < args.Length)
            {
                presetName = args[++i];
            }
            else
            {
                files.Add(args[i]);
            }
        }

        if (presetName is null)
        {
            Console.Error.WriteLine("Preset name not specified. Use --name \"PresetName\".");
            return 1;
        }

        var preset = settings.Presets.FirstOrDefault(
            p => p.Name.Equals(presetName, StringComparison.OrdinalIgnoreCase));

        if (preset is null)
        {
            Console.Error.WriteLine($"Preset '{presetName}' not found.");
            return 1;
        }

        var cliPath = ResolveCliPath(settings.CliPath);
        var quotedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));

        var arguments = $"rename --pattern \"{preset.RenamePattern}\" --files {quotedFiles}";

        if (!string.IsNullOrWhiteSpace(preset.OutputFolder))
        {
            arguments += $" --output \"{preset.OutputFolder}\"";
        }

        return LaunchCli(cliPath, arguments);
    }

    private static int LaunchCli(string cliPath, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                Console.Error.WriteLine("Failed to start MediaMatch CLI.");
                return 1;
            }

            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to launch CLI: {ex.Message}");
            return 1;
        }
    }

    private static string ResolveCliPath(string configured)
    {
        // If configured path exists, use it
        if (File.Exists(configured))
            return Path.GetFullPath(configured);

        // Check alongside this executable
        var sameDir = Path.Combine(AppContext.BaseDirectory, "MediaMatch.CLI.exe");
        if (File.Exists(sameDir))
            return sameDir;

        // Fall back to configured path (will fail at launch with a clear error)
        return configured;
    }

    private static int HandleUnknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("MediaMatch Shell Extension");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  MediaMatch.ShellExtension.exe install     Register context menu");
        Console.WriteLine("  MediaMatch.ShellExtension.exe uninstall   Remove context menu");
        Console.WriteLine("  MediaMatch.ShellExtension.exe rename <files...>");
        Console.WriteLine("  MediaMatch.ShellExtension.exe match <files...>");
        Console.WriteLine("  MediaMatch.ShellExtension.exe organize <files...>");
        Console.WriteLine("  MediaMatch.ShellExtension.exe preset --name \"Name\" <files...>");
    }
}
