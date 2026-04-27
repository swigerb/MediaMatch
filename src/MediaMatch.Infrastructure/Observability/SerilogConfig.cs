using Serilog;
using Serilog.Events;

namespace MediaMatch.Infrastructure.Observability;

/// <summary>
/// Configures Serilog logging for MediaMatch.
/// File sink uses structured JSON with daily rolling and 14-day retention.
/// Console sink uses colored human-readable output for CLI usage.
/// </summary>
public static class SerilogConfig
{
    private const string ConsoleTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Creates a fully configured Serilog <see cref="LoggerConfiguration"/>.
    /// </summary>
    /// <param name="enableConsole">Whether to enable the console sink (for CLI/debug).</param>
    /// <param name="minimumLevel">Minimum log level. Defaults to Information.</param>
    public static LoggerConfiguration CreateConfiguration(
        bool enableConsole = true,
        LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaMatch",
            "logs");

        var config = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "MediaMatch")
            .WriteTo.File(
                path: Path.Combine(logDirectory, "mediamatch-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                formatter: new Serilog.Formatting.Json.JsonFormatter(),
                shared: true);

        if (enableConsole)
        {
            config.WriteTo.Console(outputTemplate: ConsoleTemplate);
        }

        return config;
    }

    /// <summary>
    /// Initializes the global Serilog logger. Call early in app startup.
    /// </summary>
    public static void Initialize(bool enableConsole = true, bool debugMode = false)
    {
        var level = debugMode ? LogEventLevel.Debug : LogEventLevel.Information;
        Log.Logger = CreateConfiguration(enableConsole, level).CreateLogger();
    }
}
