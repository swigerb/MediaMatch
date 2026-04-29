using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.macOS.Dialogs;

/// <summary>
/// ContentDialog for selecting among opportunistic match candidates.
/// </summary>
public sealed partial class MatchSelectionDialog : ContentDialog
{
    public MatchSelectionViewModel ViewModel { get; }

    public MatchSelectionDialog(MatchSelectionViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        FileNameRun.Text = viewModel.FileName;
        SuggestionsList.ItemsSource = viewModel.Suggestions;

        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (SuggestionsList.SelectedItem is MatchSuggestionItem item)
        {
            ViewModel.SelectedMatch = item.Suggestion;
        }
        else
        {
            args.Cancel = true;
        }
    }
}
