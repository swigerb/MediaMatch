using MediaMatch.Application.Detection;
using MediaMatch.Application.Expressions;
using MediaMatch.Application.Pipeline;
using MediaMatch.Application.Services;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Expressions;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using MediaMatch.Infrastructure;
using MediaMatch.Infrastructure.Persistence;
using MediaMatch.Infrastructure.Unix;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;

namespace MediaMatch.App.Linux;

/// <summary>
/// Linux application entry point. Mirrors the macOS/Windows App.xaml.cs composition root
/// but uses Unix-specific infrastructure services (AES encryption, POSIX hard links).
/// </summary>
public partial class App : Microsoft.UI.Xaml.Application
{
    private static IServiceProvider _serviceProvider = null!;

    /// <summary>
    /// Gets the main window instance.
    /// </summary>
    public static MainWindow MainWindow { get; private set; } = null!;

    /// <summary>
    /// Resolves a service from the DI container.
    /// </summary>
    public static T GetService<T>() where T : class
        => _serviceProvider.GetRequiredService<T>();

    public App()
    {
        InitializeComponent();
        _serviceProvider = ConfigureServices();
        Log.Information("MediaMatch Linux application started");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();

        // On first launch, navigate to Settings
        var settingsRepo = _serviceProvider.GetRequiredService<ISettingsRepository>();
        if (!settingsRepo.SettingsFileExists())
        {
            MainWindow.NavigateToSettings();
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging via Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MediaMatch", "logs", "mediamatch-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Unix-specific infrastructure (AES encryption, POSIX hard links)
        services.AddUnixInfrastructure();

        // Settings persistence (uses AesFileEncryption via DI)
        var encryption = services.BuildServiceProvider().GetRequiredService<ISettingsEncryption>();
        var settingsRepo = new SettingsRepository(encryption);
        services.AddSingleton<ISettingsRepository>(settingsRepo);

        // Load persisted settings
        var savedSettings = settingsRepo.SettingsFileExists()
            ? settingsRepo.LoadAsync().GetAwaiter().GetResult()
            : new AppSettings();

        var apiConfig = new ApiConfiguration
        {
            TmdbApiKey = savedSettings.ApiKeys.TmdbApiKey,
            TvdbApiKey = savedSettings.ApiKeys.TvdbApiKey
        };

        // Register shared infrastructure services (HTTP clients, providers, etc.)
        services.AddMediaMatchInfrastructure(apiConfig);

        // Application-layer services
        services.AddSingleton<IExpressionEngine, ExpressionEngine>();
        services.AddSingleton<IMediaTypeDetector, MediaTypeDetector>();
        services.AddSingleton<IMetadataProviderChain, MetadataProviderChain>();
        services.AddSingleton<IMatchingPipeline, MatchingPipeline>();
        services.AddSingleton<IFileOrganizationService, FileOrganizationService>();
        services.AddSingleton<IBatchOperationService, BatchOperationService>();
        services.AddSingleton<IRenamePreviewService, RenamePreviewService>();
        services.AddSingleton<IUndoService, UndoService>();
        services.AddSingleton<IChecksumService, ChecksumService>();
        services.AddSingleton<ISubtitleDownloadService, SubtitleDownloadService>();

        return services.BuildServiceProvider();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled exception");
        e.Handled = true;
    }
}
