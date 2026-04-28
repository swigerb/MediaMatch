using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace MediaMatch.App.Controls;

public sealed partial class ListPanel : UserControl
{
    public ListPanelViewModel ViewModel { get; private set; }

    public ListPanel()
    {
        InitializeComponent();
        ViewModel = new ListPanelViewModel();
        try
        {
            Bindings.Update();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ListPanel binding initialization failed");
        }
    }

    public void SetViewModel(ListPanelViewModel vm)
    {
        ViewModel = vm;
        Bindings.Update();
    }

    public static bool Not(bool value) => !value;
}
