using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Web.Dialogs;

/// <summary>
/// ContentDialog for resolving file conflicts during rename operations.
/// </summary>
public sealed partial class ConflictDialog : ContentDialog
{
    public ConflictDialogViewModel ViewModel { get; }

    public ConflictDialog(ConflictDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // Populate UI from ViewModel
        SourcePathText.Text = viewModel.SourcePath;
        SourceSizeRun.Text = viewModel.SourceSize;
        SourceModifiedRun.Text = viewModel.SourceLastModified;
        TargetPathText.Text = viewModel.TargetPath;
        TargetSizeRun.Text = viewModel.TargetSize;
        TargetModifiedRun.Text = viewModel.TargetLastModified;

        PrimaryButtonClick += (_, _) => ViewModel.Resolution = ConflictResolution.Overwrite;
        SecondaryButtonClick += (_, _) => ViewModel.Resolution = ConflictResolution.Skip;
        CloseButtonClick += (_, _) => ViewModel.Resolution = ConflictResolution.CancelAll;
    }

    private void RenameButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.Resolution = ConflictResolution.RenameAppendNumber;
        Hide();
    }
}
