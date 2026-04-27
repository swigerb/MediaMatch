using System.ComponentModel;
using MediaMatch.CLI.Infrastructure;
using MediaMatch.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MediaMatch.CLI.Commands;

internal sealed class MatchSettings : CommandSettings
{
    [CommandOption("--path <PATH>")]
    [Description("Directory or file to scan for media")]
    public required string Path { get; set; }

    [CommandOption("--recursive")]
    [Description("Scan subdirectories recursively")]
    [DefaultValue(false)]
    public bool Recursive { get; set; }

    [CommandOption("--format <FORMAT>")]
    [Description("Output format: table or json")]
    [DefaultValue("table")]
    public string Format { get; set; } = "table";

    [CommandOption("--mode <MODE>")]
    [Description("Media mode: auto (default), music")]
    [DefaultValue("auto")]
    public string Mode { get; set; } = "auto";

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Path))
            return ValidationResult.Error("--path is required");

        var fullPath = System.IO.Path.GetFullPath(Path);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            return ValidationResult.Error($"Path not found: {fullPath}");

        return ValidationResult.Success();
    }
}

internal sealed class MatchCommand : AsyncCommand<MatchSettings>
{
    private readonly IMediaAnalysisService _analysisService;

    public MatchCommand(IMediaAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, MatchSettings settings, CancellationToken cancellation)
    {
        var fullPath = Path.GetFullPath(settings.Path);
        var files = MediaFileScanner.Scan(fullPath, settings.Recursive);

        if (files.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No media files found.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"[blue]Scanning {files.Count} file(s)…[/]");

        var results = await AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(false)
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Analyzing media files[/]", maxValue: files.Count);
                var analysisResults = new List<MediaAnalysisResult>(files.Count);

                foreach (var file in files)
                {
                    var result = await _analysisService.AnalyzeAsync(file);
                    analysisResults.Add(result);
                    task.Increment(1);
                }

                return analysisResults;
            });

        if (settings.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            RenderJson(results);
        }
        else
        {
            RenderTable(results);
        }

        return 0;
    }

    private static void RenderTable(List<MediaAnalysisResult> results)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Filename[/]")
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Confidence[/]")
            .AddColumn("[bold]Title[/]")
            .AddColumn("[bold]Season[/]")
            .AddColumn("[bold]Episode[/]")
            .AddColumn("[bold]Year[/]");

        foreach (var r in results)
        {
            var confColor = r.Confidence >= 0.7f ? "green"
                : r.Confidence >= 0.4f ? "yellow"
                : "red";

            table.AddRow(
                Markup.Escape(Path.GetFileName(r.FilePath)),
                r.MediaType.ToString(),
                $"[{confColor}]{r.Confidence:P0}[/]",
                Markup.Escape(r.CleanTitle ?? "—"),
                r.Season?.ToString() ?? "—",
                r.Episode?.ToString() ?? "—",
                r.Year?.ToString() ?? "—");
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[grey]Analyzed {results.Count} file(s).[/]");
    }

    private static void RenderJson(List<MediaAnalysisResult> results)
    {
        var items = results.Select(r => new
        {
            file = Path.GetFileName(r.FilePath),
            type = r.MediaType.ToString(),
            confidence = Math.Round(r.Confidence, 2),
            title = r.CleanTitle,
            season = r.Season,
            episode = r.Episode,
            year = r.Year,
        });

        var json = System.Text.Json.JsonSerializer.Serialize(items,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        AnsiConsole.WriteLine(json);
    }
}
