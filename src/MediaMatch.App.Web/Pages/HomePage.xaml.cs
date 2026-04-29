using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Web.Pages;

/// <summary>
/// Browser-adapted home page. The full match/rename pipeline requires direct
/// file system access, which is not available in the WASM sandbox, so this
/// page exposes only an Upload Files entry point that uses Uno's file picker
/// (backed by the browser's native file input).
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
            // Browser preview: list filenames only — no on-disk operations are possible.
            OriginalList.ItemsSource = files.Select(f => f.Name).ToList();
        }
    }
}
