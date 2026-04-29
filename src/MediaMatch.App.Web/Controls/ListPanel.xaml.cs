using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Web.Controls;

public sealed partial class ListPanel : UserControl
{
    public object? ViewModel
    {
        get => DataContext;
        set => DataContext = value;
    }

    public ListPanel()
    {
        InitializeComponent();
    }
}
