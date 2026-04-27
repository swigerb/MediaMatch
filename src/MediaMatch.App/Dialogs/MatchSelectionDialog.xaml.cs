using MediaMatch.Core.Models;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Dialogs;

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
            // No selection — defer to cancel (don't close with no match)
            args.Cancel = true;
        }
    }
}
