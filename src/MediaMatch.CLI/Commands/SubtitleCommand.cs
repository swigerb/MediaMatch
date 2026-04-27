using System.ComponentModel;
using MediaMatch.Core.Providers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MediaMatch.CLI.Commands;

internal sealed class SubtitleSettings : CommandSettings
{
    [CommandOption("--path <PATH>")]
    [Description("Media file to search subtitles for")]
    public required string Path { get; set; }

    [CommandOption("--lang <LANG>")]
    [Description("Subtitle language code (default: en)")]
    [DefaultValue("en")]
    public string Language { get; set; } = "en";

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Path))
            return ValidationResult.Error("--path is required");

        var fullPath = System.IO.Path.GetFullPath(Path);
        if (!File.Exists(fullPath))
            return ValidationResult.Error($"File not found: {fullPath}");

        return ValidationResult.Success();
    }
}

internal sealed class SubtitleCommand : AsyncCommand<SubtitleSettings>
{
    private readonly IEnumerable<ISubtitleProvider> _providers;

    public SubtitleCommand(IEnumerable<ISubtitleProvider> providers)
    {
        _providers = providers;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, SubtitleSettings settings, CancellationToken cancellation)
    {
        var fullPath = Path.GetFullPath(settings.Path);
        var fileName = Path.GetFileName(fullPath);

        AnsiConsole.MarkupLine($"[blue]Searching subtitles for[/] [yellow]{Markup.Escape(fileName)}[/] [blue](lang: {Markup.Escape(settings.Language)})[/]");

        var providers = _providers.ToList();
        if (providers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ No subtitle providers configured.[/]");
            AnsiConsole.MarkupLine("[grey]Subtitle search requires external provider integrations (e.g. OpenSubtitles).[/]");
            AnsiConsole.MarkupLine("[grey]This feature will be available in a future release.[/]");
            return 0;
        }

        var allResults = new List<Core.Models.SubtitleDescriptor>();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Searching providers…", async ctx =>
            {
                foreach (var provider in providers)
                {
                    ctx.Status($"Searching [blue]{provider.Name}[/]…");

                    try
                    {
                        var query = Path.GetFileNameWithoutExtension(fullPath);
                        var results = await provider.SearchAsync(query, settings.Language);
                        allResults.AddRange(results);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] {provider.Name}: {Markup.Escape(ex.Message)}");
                    }
                }
            });

        if (allResults.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No subtitles found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Language[/]")
            .AddColumn("[bold]Format[/]")
            .AddColumn("[bold]Provider[/]")
            .AddColumn("[bold]Downloads[/]");

        foreach (var sub in allResults.OrderByDescending(s => s.Downloads ?? 0))
        {
            table.AddRow(
                Markup.Escape(sub.Name),
                Markup.Escape(sub.Language),
                sub.Format.ToString(),
                Markup.Escape(sub.ProviderName ?? "—"),
                sub.Downloads?.ToString("N0") ?? "—");
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[green]{allResults.Count} subtitle(s) found.[/]");

        return 0;
    }
}
