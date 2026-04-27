using MediaMatch.App.Services;
using MediaMatch.App.ViewModels;
using MediaMatch.Application.Services;
using MediaMatch.Core.Services;
using MediaMatch.Infrastructure;
using MediaMatch.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;

namespace MediaMatch.App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Microsoft.UI.Xaml.Application
{
    private static IServiceProvider _serviceProvider = null!;

    /// <summary>
    /// Gets the main window instance for HWND access (folder pickers, etc.).
    /// </summary>
    public static MainWindow MainWindow { get; private set; } = null!;

    /// <summary>
    /// Resolves a service from the DI container.
    /// </summary>
    public static T GetService<T>() where T : class
        => _serviceProvider.GetRequiredService<T>();

    public App()
    {
        // Initialize Serilog as early as possible
        SerilogConfig.Initialize(enableConsole: true, debugMode: false);

        InitializeComponent();

        // Global unhandled exception handler
        UnhandledException += OnUnhandledException;

        _serviceProvider = ConfigureServices();

        Log.Information("MediaMatch application started");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();

        // Fire-and-forget update check — don't block app launch
        _ = CheckForUpdatesAsync();
    }

    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            var updateService = _serviceProvider.GetRequiredService<IUpdateCheckService>();
            await updateService.CheckForUpdatesAsync();

            if (updateService.IsUpdateAvailable)
            {
                Log.Information("Update available: v{Version}", updateService.LatestVersion);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Background update check failed");
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Serilog as the ILoggerFactory backend
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Navigation
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

        // Application services
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddSingleton<IBatchOperationService, BatchOperationService>();
        services.AddSingleton<IUndoService, UndoService>();

        // ViewModels
        services.AddSingleton<HomeViewModel>(sp => new HomeViewModel(
            sp.GetRequiredService<IBatchOperationService>(),
            sp.GetRequiredService<IUndoService>(),
            sp.GetRequiredService<ILogger<HomeViewModel>>()));
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();

        // Update services
        services.AddSingleton<IUpdateCheckService, UpdateCheckService>();
        services.AddTransient<UpdateViewModel>();

        return services.BuildServiceProvider();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled exception");
        Log.CloseAndFlush();
    }
}