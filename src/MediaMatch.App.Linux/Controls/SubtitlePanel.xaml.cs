using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Linux.Controls;

public sealed partial class SubtitlePanel : UserControl
{
    public object? ViewModel
    {
        get => DataContext;
        set => DataContext = value;
    }

    public SubtitlePanel()
    {
        InitializeComponent();
    }
}
