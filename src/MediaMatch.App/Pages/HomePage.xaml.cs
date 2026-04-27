using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
    }
}
