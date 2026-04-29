using MediaMatch.App.Web.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace MediaMatch.App.Web.Dialogs;

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

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        if (!string.IsNullOrEmpty(filePath))
        {
            Loaded += async (_, _) => await ViewModel.LoadFileCommand.ExecuteAsync(filePath);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            FilePathText.Text = ViewModel.FilePath;

            // Loading
            var isLoading = ViewModel.IsLoading;
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            LoadingRing.IsActive = isLoading;

            // Error
            var hasError = !string.IsNullOrEmpty(ViewModel.ErrorMessage);
            ErrorInfoBar.IsOpen = hasError;
            ErrorInfoBar.Message = ViewModel.ErrorMessage;
            ErrorInfoBar.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;

            // Content
            var hasResult = ViewModel.HasResult;
            ContentPivot.Visibility = hasResult ? Visibility.Visible : Visibility.Collapsed;
            CopyPanel.Visibility = hasResult ? Visibility.Visible : Visibility.Collapsed;

            GeneralList.ItemsSource = ViewModel.GeneralProperties;
            VideoList.ItemsSource = ViewModel.VideoStreams;
            AudioList.ItemsSource = ViewModel.AudioStreams;
            SubtitlesList.ItemsSource = ViewModel.TextStreams;
        });
    }

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");

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
}
