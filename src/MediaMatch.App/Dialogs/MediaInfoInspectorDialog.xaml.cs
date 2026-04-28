using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace MediaMatch.App.Dialogs;

/// <summary>
/// ContentDialog for inspecting media file properties via ffprobe.
/// </summary>
public sealed partial class MediaInfoInspectorDialog : ContentDialog
{
    public MediaInfoInspectorViewModel ViewModel { get; }

    public MediaInfoInspectorDialog(MediaInfoInspectorViewModel viewModel, string? filePath = null)
    {
        ViewModel = viewModel;
        InitializeComponent();

        if (!string.IsNullOrEmpty(filePath))
        {
            Loaded += async (_, _) => await ViewModel.LoadFileCommand.ExecuteAsync(filePath);
        }
    }

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");

        // Initialize the picker with the app window handle
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await ViewModel.LoadFileCommand.ExecuteAsync(file.Path);
        }
    }

    private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyToClipboardCommand.Execute(null);
    }

    // x:Bind helpers
    public Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public bool HasError(string errorMessage) =>
        !string.IsNullOrEmpty(errorMessage);

    public Visibility ErrorVisibility(string errorMessage) =>
        string.IsNullOrEmpty(errorMessage) ? Visibility.Collapsed : Visibility.Visible;
}
