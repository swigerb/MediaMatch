using MediaMatch.App.Dialogs;
using MediaMatch.App.Pages;
using MediaMatch.App.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace MediaMatch.App;

public sealed partial class MainWindow : Window
{
    private readonly DispatcherTimer _splashTimer;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Set version text
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionString = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
        VersionText.Text = versionString;
        SplashVersionText.Text = versionString;

        // Wire navigation service to the frame
        var navigationService = App.GetService<NavigationService>();
        navigationService.SetFrame(NavFrame);

        // Navigate to Home on startup
        try
        {
            NavFrame.Navigate(typeof(HomePage));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to navigate to HomePage on startup");
        }

        // Start splash fade-out timer
        _splashTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.8) };
        _splashTimer.Tick += SplashTimer_Tick;
        _splashTimer.Start();
    }

    private void SplashTimer_Tick(object? sender, object e)
    {
        _splashTimer.Stop();
        SplashFadeOut.Completed += (_, _) =>
        {
            SplashOverlay.Visibility = Visibility.Collapsed;
        };
        SplashFadeOut.Begin();
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        try
        {
            if (args.IsSettingsSelected)
            {
                NavFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItem is NavigationViewItem item)
            {
                switch (item.Tag)
                {
                    case "home":
                        NavFrame.Navigate(typeof(HomePage));
                        break;
                    case "history":
                        NavFrame.Navigate(typeof(HistoryPage));
                        break;
                    case "about":
                        NavFrame.Navigate(typeof(AboutPage));
                        break;
                    default:
                        Log.Warning("Unknown navigation item tag: {Tag}", item.Tag);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Navigation failed");
        }
    }

    /// <summary>
    /// Programmatically navigates to the Settings page (used for first-run detection).
    /// </summary>
    public void NavigateToSettings(bool firstRun = false)
    {
        NavFrame.Navigate(typeof(SettingsPage), firstRun ? "first-run" : null);
    }

    private async void LogViewer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new LogViewerDialog { XamlRoot = Content.XamlRoot };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open log viewer");
        }
    }
}
