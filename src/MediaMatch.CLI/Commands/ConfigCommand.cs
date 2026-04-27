using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MediaMatch.CLI.Commands;

internal sealed class ConfigSetSettings : CommandSettings
{
    [CommandArgument(0, "<KEY>")]
    [Description("Configuration key (e.g. tmdb_api_key, tvdb_api_key, rename_pattern)")]
    public required string Key { get; set; }

    [CommandArgument(1, "<VALUE>")]
    [Description("Value to set")]
    public required string Value { get; set; }
}

internal sealed class ConfigGetSettings : CommandSettings
{
    [CommandArgument(0, "<KEY>")]
    [Description("Configuration key to read")]
    public required string Key { get; set; }
}

internal sealed class ConfigSetCommand : Command<ConfigSetSettings>
{
    protected override int Execute(CommandContext context, ConfigSetSettings settings, CancellationToken cancellation)
    {
        var config = ConfigStore.Load();
        config[settings.Key] = settings.Value;
        ConfigStore.Save(config);

        AnsiConsole.MarkupLine($"[green]✓[/] Set [blue]{Markup.Escape(settings.Key)}[/] = [yellow]{Markup.Escape(settings.Value)}[/]");
        return 0;
    }
}

internal sealed class ConfigGetCommand : Command<ConfigGetSettings>
{
    protected override int Execute(CommandContext context, ConfigGetSettings settings, CancellationToken cancellation)
    {
        var config = ConfigStore.Load();

        if (config.TryGetValue(settings.Key, out var value))
        {
            AnsiConsole.MarkupLine($"[blue]{Markup.Escape(settings.Key)}[/] = [yellow]{Markup.Escape(value)}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Key '{Markup.Escape(settings.Key)}' not found.[/]");
            return 1;
        }

        return 0;
    }
}

internal sealed class ConfigListSettings : CommandSettings { }

internal sealed class ConfigListCommand : Command<ConfigListSettings>
{
    protected override int Execute(CommandContext context, ConfigListSettings settings, CancellationToken cancellation)
    {
        var config = ConfigStore.Load();

        if (config.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No configuration values set.[/]");
            AnsiConsole.MarkupLine("[grey]Use 'mediamatch config set <key> <value>' to add settings.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Key[/]")
            .AddColumn("[bold]Value[/]");

        foreach (var (key, value) in config.OrderBy(kv => kv.Key))
        {
            var display = key.Contains("key", StringComparison.OrdinalIgnoreCase)
                ? MaskValue(value)
                : value;

            table.AddRow(
                Markup.Escape(key),
                Markup.Escape(display));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[grey]Config file: {Markup.Escape(ConfigStore.ConfigPath)}[/]");
        return 0;
    }

    private static string MaskValue(string value) =>
        value.Length <= 4 ? "****" : string.Concat(value.AsSpan(0, 4), "****");
}

/// <summary>
/// Simple JSON-based configuration store at %LOCALAPPDATA%/MediaMatch/config.json.
/// </summary>
internal static class ConfigStore
{
    public static string ConfigPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MediaMatch", "config.json");

    public static Dictionary<string, string> Load()
    {
        if (!File.Exists(ConfigPath))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var node = JsonNode.Parse(json);
            if (node is JsonObject obj)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in obj)
                {
                    if (prop.Value is not null)
                        dict[prop.Key] = prop.Value.ToString();
                }
                return dict;
            }
        }
        catch
        {
            // Corrupt file — start fresh
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static void Save(Dictionary<string, string> config)
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);

        var obj = new JsonObject();
        foreach (var (key, value) in config.OrderBy(kv => kv.Key))
            obj[key] = value;

        var json = obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
