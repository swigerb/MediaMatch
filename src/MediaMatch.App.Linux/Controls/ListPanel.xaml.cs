using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Linux.Controls;

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
