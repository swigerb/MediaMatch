using MediaMatch.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Spectre.Console;

AnsiConsole.Write(
    new FigletText("MediaMatch")
        .Color(Color.CornflowerBlue));

AnsiConsole.MarkupLine("[grey]v0.1.0 — Modern media file organizer[/]");
AnsiConsole.WriteLine();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaMatch", "logs", "mediamatch-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    
    builder.Services.AddSerilog();
    
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddHttpClientInstrumentation())
        .WithMetrics(metrics => metrics
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation());
    
    builder.Services.AddMediaMatchInfrastructure();
    
    var host = builder.Build();
    
    AnsiConsole.MarkupLine("[green]✓[/] MediaMatch initialized successfully.");
    AnsiConsole.MarkupLine("[grey]Use --help for available commands.[/]");
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

return 0;
