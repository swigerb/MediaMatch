using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Web.Pages;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();

        // Resolve AboutViewModel from DI when available.
        // For now, DataContext can be set externally or via navigation parameter.
    }
}
