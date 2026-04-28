using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Controls;

public sealed partial class EpisodesPanel : UserControl
{
    public EpisodesPanelViewModel ViewModel { get; private set; }

    public EpisodesPanel()
    {
        InitializeComponent();
        ViewModel = new EpisodesPanelViewModel();
        Bindings.Update();
    }

    public void SetViewModel(EpisodesPanelViewModel vm)
    {
        ViewModel = vm;
        Bindings.Update();
    }
}
