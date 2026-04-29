using MediaMatch.Core.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.macOS.Dialogs;

/// <summary>
/// ContentDialog for managing the list of presets (add / edit / delete).
/// Returns the updated list via <see cref="Presets"/> when the user clicks Save.
/// </summary>
public sealed partial class PresetManagerDialog : ContentDialog
{
    private readonly List<PresetDefinitionSettings> _presets;

    /// <summary>The current preset list — updated in-place by add/edit/delete actions.</summary>
    public List<PresetDefinitionSettings> Presets => _presets;

    public PresetManagerDialog(List<PresetDefinitionSettings> presets)
    {
        _presets = [.. presets];
        InitializeComponent();
        RefreshList();
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
