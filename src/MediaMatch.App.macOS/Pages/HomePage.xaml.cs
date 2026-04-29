using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.macOS.Pages;

/// <summary>
/// Home page with dual-pane file list and toolbar.
/// </summary>
public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void LoadFiles_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add("*");

        var files = await picker.PickMultipleFilesAsync();
        if (files is { Count: > 0 })
        {
            // TODO: Wire to ViewModel and populate file lists
        }
    }
}
