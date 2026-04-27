using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Pages;

public sealed partial class AboutPage : Page
{
    public AboutViewModel ViewModel { get; }

    public AboutPage()
    {
        ViewModel = App.GetService<AboutViewModel>();
        InitializeComponent();
    }
}
