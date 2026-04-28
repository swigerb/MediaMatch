using System.ComponentModel;
using MediaMatch.CLI.Infrastructure;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MediaMatch.CLI.Commands;

/// <summary>
/// Command settings for the <c>rename</c> CLI command.
/// </summary>
internal sealed class RenameSettings : CommandSettings
{
    /// <summary>Gets or sets the directory or file to rename.</summary>
    [CommandOption("--path <PATH>")]
    [Description("Directory or file to rename")]
    public required string Path { get; set; }

    /// <summary>Gets or sets the rename template pattern.</summary>
    [CommandOption("--pattern <PATTERN>")]
    [Description("Rename template (FileBot-compatible), e.g. \"{n} - {s00e00} - {t}\"")]
    [DefaultValue("{n} - {s00e00} - {t}")]
    public string Pattern { get; set; } = "{n} - {s00e00} - {t}";

    /// <summary>Gets or sets a value indicating whether to preview only without renaming.</summary>
    [CommandOption("--dry-run")]
    [Description("Preview only — no files will be renamed")]
    [DefaultValue(false)]
    public bool DryRun { get; set; }

    /// <summary>Gets or sets a value indicating whether to scan subdirectories recursively.</summary>
    [CommandOption("--recursive")]
    [Description("Scan subdirectories recursively")]
    [DefaultValue(false)]
    public bool Recursive { get; set; }

    /// <summary>Gets or sets the output format (table or json).</summary>
    [CommandOption("--format <FORMAT>")]
    [Description("Output format: table or json")]
    [DefaultValue("table")]
    public string Format { get; set; } = "table";

    /// <summary>Gets or sets the file action type (move, copy, clone, hardlink, test).</summary>
    [CommandOption("--action <ACTION>")]
    [Description("File action: move (default), copy, clone, hardlink, test")]
    [DefaultValue("move")]
    public string Action { get; set; } = "move";

    /// <summary>Gets or sets the media detection mode (auto or music).</summary>
    [CommandOption("--mode <MODE>")]
    [Description("Media mode: auto (default), music")]
    [DefaultValue("auto")]
    public string Mode { get; set; } = "auto";

    /// <summary>Gets or sets the comma-separated post-process actions to run.</summary>
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

/// <summary>
/// CLI command that renames media files using metadata lookup.
/// </summary>
internal sealed class RenameCommand : AsyncCommand<RenameSettings>
{
    private readonly IRenamePreviewService _previewService;
    private readonly IFileOrganizationService _organizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenameCommand"/> class.
    /// </summary>
    /// <param name="previewService">The rename preview service.</param>
    /// <param name="organizationService">The file organization service.</param>
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
                    var preview = await _previewService.PreviewAsync(files, settings.Pattern).ConfigureAwait(false);
                    items = preview.ToArray();
                }
                else
                {
                    var organized = await _organizationService.OrganizeAsync(files, settings.Pattern, action).ConfigureAwait(false);
                    items = organized.ToArray();
                }

                task.Value = files.Count;
                return items;
            }).ConfigureAwait(false);

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
