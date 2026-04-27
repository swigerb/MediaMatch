using MediaMatch.Application.Expressions;
using MediaMatch.Application.Pipeline;
using MediaMatch.Application.Services;
using MediaMatch.CLI.Commands;
using MediaMatch.CLI.Infrastructure;
using MediaMatch.Core.Expressions;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using MediaMatch.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .WriteTo.File(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaMatch", "logs", "mediamatch-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    var services = new ServiceCollection();

    // Logging
    services.AddLogging(builder => builder.AddSerilog(dispose: true));

    // OpenTelemetry
    services.AddOpenTelemetry()
        .WithTracing(tracing => tracing.AddHttpClientInstrumentation())
        .WithMetrics(metrics => metrics
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation());

    // Infrastructure (HTTP clients, caching, metadata providers)
    services.AddMediaMatchInfrastructure();

    // Application services
    services.AddSingleton<IExpressionEngine, ScribanExpressionEngine>();
    services.AddSingleton<IMatchingPipeline, MatchingPipeline>();
    services.AddSingleton<IRenamePreviewService, RenamePreviewService>();
    services.AddSingleton<IMediaAnalysisService, MediaAnalysisService>();
    services.AddSingleton<IFileSystem, PhysicalFileSystem>();
    services.AddSingleton<IFileOrganizationService, FileOrganizationService>();

    // Subtitle providers — none yet; register empty enumerable
    services.AddSingleton<IEnumerable<ISubtitleProvider>>(
        _ => Enumerable.Empty<ISubtitleProvider>());

    var registrar = new TypeRegistrar(services);
    var app = new CommandApp(registrar);

    app.Configure(config =>
    {
        config.SetApplicationName("mediamatch");
        config.SetApplicationVersion("0.1.0");

        config.AddCommand<MatchCommand>("match")
            .WithDescription("Detect media type for files in a directory")
            .WithExample("match", "--path", "C:\\TV Shows", "--recursive");

        config.AddCommand<RenameCommand>("rename")
            .WithDescription("Rename media files using metadata lookup")
            .WithExample("rename", "--path", "C:\\TV Shows", "--dry-run")
            .WithExample("rename", "--path", ".", "--pattern", "{n} ({y})");

        config.AddBranch("config", cfg =>
        {
            cfg.SetDescription("Manage MediaMatch configuration");

            cfg.AddCommand<ConfigSetCommand>("set")
                .WithDescription("Set a configuration value")
                .WithExample("config", "set", "tmdb_api_key", "abc123");

            cfg.AddCommand<ConfigGetCommand>("get")
                .WithDescription("Get a configuration value")
                .WithExample("config", "get", "tmdb_api_key");

            cfg.AddCommand<ConfigListCommand>("list")
                .WithDescription("List all configuration values");
        });

        config.AddCommand<SubtitleCommand>("subtitle")
            .WithDescription("Search for subtitles for a media file")
            .WithExample("subtitle", "--path", "movie.mkv", "--lang", "en");
    });

    return await app.RunAsync(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    AnsiConsole.WriteException(ex);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
