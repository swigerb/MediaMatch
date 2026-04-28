using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Pages;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; }

    public HistoryPage()
    {
        ViewModel = App.GetService<HistoryViewModel>();
        DataContext = ViewModel;
        InitializeComponent();

        Loaded += async (_, _) => await ViewModel.LoadHistoryCommand.ExecuteAsync(null);
    }
}
