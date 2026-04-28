using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace MediaMatch.App.Controls;

public sealed partial class FilterPanel : UserControl
{
    public FilterPanelViewModel ViewModel { get; private set; }

    public FilterPanel()
    {
        InitializeComponent();
        ViewModel = new FilterPanelViewModel();
        try
        {
            Bindings.Update();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "FilterPanel binding initialization failed");
        }
    }

    public void SetViewModel(FilterPanelViewModel vm)
    {
        ViewModel = vm;
        Bindings.Update();
    }

    // Tab visibility helpers
    public Visibility IsMediaInfoTab =>
        ViewModel.SelectedTabIndex == 4 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsNotMediaInfoTab =>
        ViewModel.SelectedTabIndex != 4 ? Visibility.Visible : Visibility.Collapsed;

    // Tab radio button handlers
    private void SetTab0(object sender, RoutedEventArgs e) { ViewModel.SelectedTabIndex = 0; Bindings.Update(); }
    private void SetTab1(object sender, RoutedEventArgs e) { ViewModel.SelectedTabIndex = 1; Bindings.Update(); }
    private void SetTab2(object sender, RoutedEventArgs e) { ViewModel.SelectedTabIndex = 2; Bindings.Update(); }
    private void SetTab3(object sender, RoutedEventArgs e) { ViewModel.SelectedTabIndex = 3; Bindings.Update(); }
    private void SetTab4(object sender, RoutedEventArgs e) { ViewModel.SelectedTabIndex = 4; Bindings.Update(); }
}
