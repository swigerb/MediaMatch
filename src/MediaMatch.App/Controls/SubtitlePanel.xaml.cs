using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Controls;

public sealed partial class SubtitlePanel : UserControl
{
    public SubtitlePanelViewModel ViewModel { get; private set; }

    public SubtitlePanel()
    {
        ViewModel = new SubtitlePanelViewModel();
        InitializeComponent();
    }

    public void SetViewModel(SubtitlePanelViewModel vm)
    {
        ViewModel = vm;
        Bindings.Update();
    }
}
