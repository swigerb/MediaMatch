using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace MediaMatch.App.Controls;

public sealed partial class EpisodesPanel : UserControl
{
    public EpisodesPanelViewModel ViewModel { get; private set; }

    public EpisodesPanel()
    {
        InitializeComponent();
        ViewModel = new EpisodesPanelViewModel();
        try
        {
            Bindings.Update();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "EpisodesPanel binding initialization failed");
        }
    }

    public void SetViewModel(EpisodesPanelViewModel vm)
    {
        ViewModel = vm;
        Bindings.Update();
    }
}
