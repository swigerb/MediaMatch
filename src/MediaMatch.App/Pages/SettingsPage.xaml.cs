using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }
}
