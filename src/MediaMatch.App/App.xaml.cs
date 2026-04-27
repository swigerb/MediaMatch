using MediaMatch.App.Services;
using MediaMatch.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

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
        InitializeComponent();
        _serviceProvider = ConfigureServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Navigation
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

        // ViewModels — transient so each page gets a fresh instance if navigated again
        services.AddSingleton<HomeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();

        return services.BuildServiceProvider();
    }
}
