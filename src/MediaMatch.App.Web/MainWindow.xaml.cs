using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MediaMatch.App.Web.Pages;

namespace MediaMatch.App.Web;

/// <summary>
/// Main window with NavigationView shell for the WebAssembly app.
/// No Mica backdrop or title bar customization — uses platform-native chrome.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "MediaMatch";

        // Navigate to Home on launch
        ContentFrame.Navigate(typeof(HomePage));
    }

    /// <summary>
    /// Navigates to the Settings page (used on first-run).
    /// </summary>
    public void NavigateToSettings()
    {
        NavView.SelectedItem = NavView.SettingsItem;
        ContentFrame.Navigate(typeof(SettingsPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            switch (tag)
            {
                case "home":
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                case "history":
                    ContentFrame.Navigate(typeof(HistoryPage));
                    break;
            }
        }
    }
}
