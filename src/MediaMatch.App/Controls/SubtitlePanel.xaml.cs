using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace MediaMatch.App.Controls;

public sealed partial class SubtitlePanel : UserControl
{
    public SubtitlePanelViewModel ViewModel { get; private set; }

    public SubtitlePanel()
    {
        InitializeComponent();
        ViewModel = new SubtitlePanelViewModel();
        try
        {
            Bindings.Update();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SubtitlePanel binding initialization failed");
        }
    }

    public void SetViewModel(SubtitlePanelViewModel vm)
    {
        ViewModel = vm;
        Bindings.Update();
    }
}
