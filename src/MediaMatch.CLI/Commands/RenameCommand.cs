using System.ComponentModel;
using MediaMatch.CLI.Infrastructure;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MediaMatch.CLI.Commands;

internal sealed class RenameSettings : CommandSettings
{
    [CommandOption("--path <PATH>")]
    [Description("Directory or file to rename")]
    public required string Path { get; set; }

    [CommandOption("--pattern <PATTERN>")]
    [Description("Rename template (FileBot-compatible), e.g. \"{n} - {s00e00} - {t}\"")]
    [DefaultValue("{n} - {s00e00} - {t}")]
    public string Pattern { get; set; } = "{n} - {s00e00} - {t}";

    [CommandOption("--dry-run")]
    [Description("Preview only — no files will be renamed")]
    [DefaultValue(false)]
    public bool DryRun { get; set; }

    [CommandOption("--recursive")]
    [Description("Scan subdirectories recursively")]
    [DefaultValue(false)]
    public bool Recursive { get; set; }

    [CommandOption("--format <FORMAT>")]
    [Description("Output format: table or json")]
    [DefaultValue("table")]
    public string Format { get; set; } = "table";

    [CommandOption("--action <ACTION>")]
    [Description("File action: move (default), copy, clone, hardlink, test")]
    [DefaultValue("move")]
    public string Action { get; set; } = "move";

    [CommandOption("--mode <MODE>")]
    [Description("Media mode: auto (default), music")]
    [DefaultValue("auto")]
    public string Mode { get; set; } = "auto";

    [CommandOption("--apply <ACTIONS>")]
    [Description("Comma-separated post-process actions to run (e.g., plex-refresh,thumbnail)")]
    public string? Apply { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Path))
            return ValidationResult.Error("--path is required");

        var fullPath = System.IO.Path.GetFullPath(Path);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            return ValidationResult.Error($"Path not found: {fullPath}");

        if (!Enum.TryParse<MediaMatch.Core.Enums.RenameAction>(Action, ignoreCase: true, out _))
            return ValidationResult.Error($"Invalid action: {Action}. Valid options: move, copy, clone, hardlink, test");

        return ValidationResult.Success();
    }
}

internal sealed class RenameCommand : AsyncCommand<RenameSettings>
{
    private readonly IRenamePreviewService _previewService;
    private readonly IFileOrganizationService _organizationService;

    public RenameCommand(
        IRenamePreviewService previewService,
        IFileOrganizationService organizationService)
    {
        _previewService = previewService;
        _organizationService = organizationService;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, RenameSettings settings, CancellationToken cancellation)
    {
        var fullPath = Path.GetFullPath(settings.Path);
        var files = MediaFileScanner.Scan(fullPath, settings.Recursive);

        if (files.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No media files found.[/]");
            return 0;
        }

        var action = Enum.Parse<Core.Enums.RenameAction>(settings.Action, ignoreCase: true);
        var isDryRun = settings.DryRun || action == Core.Enums.RenameAction.Test;

        var mode = isDryRun ? "[cyan]DRY RUN[/]" : $"[green]{action.ToString().ToUpperInvariant()}[/]";
        AnsiConsole.MarkupLine($"{mode} — Processing {files.Count} file(s) with pattern [blue]{Markup.Escape(settings.Pattern)}[/]");

        var results = await AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(false)
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Processing files[/]", maxValue: files.Count);

                Core.Models.FileOrganizationResult[] items;
                if (isDryRun)
                {
                    var preview = await _previewService.PreviewAsync(files, settings.Pattern);
                    items = preview.ToArray();
                }
                else
                {
                    var organized = await _organizationService.OrganizeAsync(files, settings.Pattern, action);
                    items = organized.ToArray();
                }

                task.Value = files.Count;
                return items;
            });

        if (settings.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            RenderJson(results, isDryRun);
        }
        else
        {
            RenderTable(results, isDryRun);
        }

        var succeeded = results.Count(r => r.Success);
        var failed = results.Length - succeeded;

        if (failed > 0)
            AnsiConsole.MarkupLine($"\n[green]{succeeded} succeeded[/], [red]{failed} failed[/]");
        else
            AnsiConsole.MarkupLine($"\n[green]All {succeeded} file(s) processed successfully.[/]");

        return failed > 0 ? 1 : 0;
    }

    private static void RenderTable(
        Core.Models.FileOrganizationResult[] results,
        bool dryRun)
    {
        var label = dryRun ? "Preview" : "Result";
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]{label}[/]")
            .AddColumn("[bold]Original[/]")
            .AddColumn("[bold]New Name[/]")
            .AddColumn("[bold]Confidence[/]")
            .AddColumn("[bold]Status[/]");

        foreach (var r in results)
        {
            var status = r.Success
                ? "[green]✓[/]"
                : $"[red]✗ {Markup.Escape(string.Join("; ", r.Warnings))}[/]";

            table.AddRow(
                Markup.Escape(Path.GetFileName(r.OriginalPath)),
                Markup.Escape(r.NewPath is not null ? Path.GetFileName(r.NewPath) : "—"),
                $"{r.MatchConfidence:P0}",
                status);
        }

        AnsiConsole.Write(table);
    }

    private static void RenderJson(
        Core.Models.FileOrganizationResult[] results,
        bool dryRun)
    {
        var items = results.Select(r => new
        {
            original = Path.GetFileName(r.OriginalPath),
            newName = r.NewPath is not null ? Path.GetFileName(r.NewPath) : null,
            confidence = Math.Round(r.MatchConfidence, 2),
            success = r.Success,
            warnings = r.Warnings,
            dryRun,
        });

        var json = System.Text.Json.JsonSerializer.Serialize(items,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        AnsiConsole.WriteLine(json);
    }
}
