using MediaMatch.App.Web.Services;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;

namespace MediaMatch.App.Web;

/// <summary>
/// WebAssembly application entry point. Mirrors the macOS/Linux App.xaml.cs composition
/// root, but omits Unix infrastructure (no DPAPI/AES key files in the browser sandbox)
/// and the Serilog file sink (no writable file system in the browser).
/// </summary>
public partial class App : Microsoft.UI.Xaml.Application
{
    private static IServiceProvider _serviceProvider = null!;

    /// <summary>Gets the main window instance.</summary>
    public static MainWindow MainWindow { get; private set; } = null!;

    /// <summary>Resolves a service from the DI container.</summary>
    public static T GetService<T>() where T : class
        => _serviceProvider.GetRequiredService<T>();

    public App()
    {
        InitializeComponent();
        _serviceProvider = ConfigureServices();
        Log.Information("MediaMatch WebAssembly application started");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();

        var settingsRepo = _serviceProvider.GetRequiredService<ISettingsRepository>();
        if (!settingsRepo.SettingsFileExists())
        {
            MainWindow.NavigateToSettings();
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Console-only Serilog (browser dev tools); no file sink in WASM.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Browser-friendly settings encryption (Base64 only — no DPAPI/AES in browser).
        services.AddSingleton<ISettingsEncryption, BrowserSettingsEncryption>();

        // Settings persistence. SettingsRepository writes under LocalApplicationData,
        // which the Mono WASM runtime maps to a per-origin virtual file system.
        var encryption = services.BuildServiceProvider().GetRequiredService<ISettingsEncryption>();
        var settingsRepo = new SettingsRepository(encryption);
        services.AddSingleton<ISettingsRepository>(settingsRepo);

        var savedSettings = settingsRepo.SettingsFileExists()
            ? settingsRepo.LoadAsync().GetAwaiter().GetResult()
            : new AppSettings();

        var apiConfig = new ApiConfiguration
        {
            TmdbApiKey = savedSettings.ApiKeys.TmdbApiKey,
            TvdbApiKey = savedSettings.ApiKeys.TvdbApiKey
        };

        // Shared infrastructure services (HTTP clients, providers, caching).
        services.AddMediaMatchInfrastructure(apiConfig);

        // Application-layer services.
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
