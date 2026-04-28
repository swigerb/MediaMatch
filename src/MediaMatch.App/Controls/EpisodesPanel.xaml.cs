using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Controls;

public sealed partial class EpisodesPanel : UserControl
{
    public EpisodesPanelViewModel ViewModel { get; private set; }

    public EpisodesPanel()
    {
        ViewModel = new EpisodesPanelViewModel();
        InitializeComponent();
    }

    public void SetViewModel(EpisodesPanelViewModel vm)
    {
        ViewModel = vm;
        Bindings.Update();
    }
}
