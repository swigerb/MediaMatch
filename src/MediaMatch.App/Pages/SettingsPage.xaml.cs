using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MediaMatch.App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.StatusMessage) &&
                !string.IsNullOrEmpty(ViewModel.StatusMessage))
            {
                ShowNotification(ViewModel.StatusMessage, ViewModel.StatusSeverity);
            }
        };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Load persisted settings when navigating to this page
        await ViewModel.LoadSettingsCommand.ExecuteAsync(null);

        // Show welcome banner if this is a first-run redirect
        if (e.Parameter is string param && param == "first-run")
        {
            ViewModel.ShowWelcomeBanner = true;
        }
    }

    private void ShowNotification(string message, InfoBarSeverity severity)
    {
        SettingsNotification.Message = message;
        SettingsNotification.Severity = severity;
        SettingsNotification.IsOpen = true;
    }
}
