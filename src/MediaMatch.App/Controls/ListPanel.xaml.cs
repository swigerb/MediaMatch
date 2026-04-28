using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Controls;

public sealed partial class ListPanel : UserControl
{
    public ListPanelViewModel ViewModel { get; private set; }

    public ListPanel()
    {
        ViewModel = new ListPanelViewModel();
        InitializeComponent();
    }

    public void SetViewModel(ListPanelViewModel vm)
    {
        ViewModel = vm;
        Bindings.Update();
    }

    public static bool Not(bool value) => !value;
}
