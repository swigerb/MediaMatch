using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Linux.Controls;

public sealed partial class EpisodesPanel : UserControl
{
    public object? ViewModel
    {
        get => DataContext;
        set => DataContext = value;
    }

    public EpisodesPanel()
    {
        InitializeComponent();
    }

    public void SetViewModel(object viewModel)
    {
        ViewModel = viewModel;
    }
}
