using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Controls;

public sealed partial class SfvPanel : UserControl
{
    public SfvPanelViewModel ViewModel { get; private set; }

    public SfvPanel()
    {
        ViewModel = new SfvPanelViewModel();
        InitializeComponent();
    }

    public void SetViewModel(SfvPanelViewModel vm)
    {
        ViewModel = vm;
        Bindings.Update();
    }

    // Helpers for conditional visibility in the item template
    public static Visibility IsInProgress(SfvState state) =>
        state == SfvState.InProgress ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility IsNotInProgress(SfvState state) =>
        state != SfvState.InProgress ? Visibility.Visible : Visibility.Collapsed;

    // Algorithm radio button handlers
    private void SetAlgorithm0(object sender, RoutedEventArgs e) => ViewModel.SelectedAlgorithmIndex = 0;
    private void SetAlgorithm1(object sender, RoutedEventArgs e) => ViewModel.SelectedAlgorithmIndex = 1;
    private void SetAlgorithm2(object sender, RoutedEventArgs e) => ViewModel.SelectedAlgorithmIndex = 2;
    private void SetAlgorithm3(object sender, RoutedEventArgs e) => ViewModel.SelectedAlgorithmIndex = 3;
    private void SetAlgorithm4(object sender, RoutedEventArgs e) => ViewModel.SelectedAlgorithmIndex = 4;
}
