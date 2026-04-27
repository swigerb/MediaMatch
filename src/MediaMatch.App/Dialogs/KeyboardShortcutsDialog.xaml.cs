using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Dialogs;

/// <summary>
/// ContentDialog displaying all available keyboard shortcuts.
/// Triggered by F1 or ? key from any page.
/// </summary>
public sealed partial class KeyboardShortcutsDialog : ContentDialog
{
    public KeyboardShortcutsDialog()
    {
        InitializeComponent();
    }
}
