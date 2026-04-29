using MediaMatch.Core.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Dialogs;

/// <summary>
/// ContentDialog for managing the list of presets (add / edit / delete).
/// Returns the updated list via <see cref="Presets"/> when the user clicks Save.
/// </summary>
/// <remarks>
/// WinUI only allows one ContentDialog open at a time. To launch the per-item editor
/// we must Hide() this dialog, which completes its own ShowAsync() with a non-Primary
/// result. <see cref="ShowManagedAsync"/> bridges around that by tracking whether the
/// Closed event came from a real user action or from an internal hide/re-show, so the
/// caller only observes the user's final Save/Cancel choice.
/// </remarks>
public sealed partial class PresetManagerDialog : ContentDialog
{
    private readonly List<PresetDefinitionSettings> _presets;
    private readonly TaskCompletionSource<ContentDialogResult> _finalResult = new();
    private bool _suppressNextClose;

    /// <summary>The current preset list — updated in-place by add/edit/delete actions.</summary>
    public List<PresetDefinitionSettings> Presets => _presets;

    public PresetManagerDialog(List<PresetDefinitionSettings> presets)
    {
        _presets = [.. presets];
        InitializeComponent();
        RefreshList();
        Closed += OnClosed;
    }

    /// <summary>
    /// Shows the manager dialog and returns only when the user has finished
    /// (Save or Cancel), even if the editor sub-dialog was opened in between.
    /// </summary>
    public Task<ContentDialogResult> ShowManagedAsync()
    {
        _ = ShowAsync();
        return _finalResult.Task;
    }

    private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        if (_suppressNextClose)
        {
            _suppressNextClose = false;
            return;
        }
        _finalResult.TrySetResult(args.Result);
    }

    private void RefreshList()
    {
        PresetList.ItemsSource = null;
        PresetList.ItemsSource = _presets;
        EmptyMessage.Visibility = _presets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void AddPreset_Click(object sender, RoutedEventArgs e)
    {
        var editor = new PresetEditorDialog
        {
            XamlRoot = this.XamlRoot
        };

        // Hide this dialog while the editor is shown (WinUI only allows one ContentDialog).
        // The Closed handler will skip this hide so the caller's task stays pending.
        _suppressNextClose = true;
        Hide();

        var result = await editor.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _presets.Add(editor.Preset);
        }

        RefreshList();
        _ = ShowAsync();
    }

    private async void EditPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PresetDefinitionSettings preset }) return;

        var index = _presets.IndexOf(preset);
        if (index < 0) return;

        var editor = new PresetEditorDialog(preset)
        {
            XamlRoot = this.XamlRoot
        };

        _suppressNextClose = true;
        Hide();

        var result = await editor.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _presets[index] = editor.Preset;
        }

        RefreshList();
        _ = ShowAsync();
    }

    private void DeletePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PresetDefinitionSettings preset }) return;
        _presets.Remove(preset);
        RefreshList();
    }
}
