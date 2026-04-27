using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Dialogs;

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
