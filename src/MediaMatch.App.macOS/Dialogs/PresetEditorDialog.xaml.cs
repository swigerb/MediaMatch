using MediaMatch.App.macOS.ViewModels;
using MediaMatch.Core.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace MediaMatch.App.macOS.Dialogs;

/// <summary>
/// ContentDialog for creating or editing a preset definition.
/// </summary>
public sealed partial class PresetEditorDialog : ContentDialog
{
    public PresetEditorViewModel ViewModel { get; }

    /// <summary>The resulting preset after the dialog is closed via Save.</summary>
    public PresetDefinitionSettings Preset => ViewModel.ToPreset();

    /// <summary>
    /// Creates a new preset editor dialog.
    /// Pass an existing preset to edit, or null to create a new one.
    /// </summary>
    public PresetEditorDialog(PresetDefinitionSettings? existingPreset = null)
    {
        ViewModel = new PresetEditorViewModel();
        InitializeComponent();

        if (existingPreset is not null)
        {
            Title = "Edit Preset";
            ViewModel.LoadFromPreset(existingPreset);
        }
        else
        {
            Title = "New Preset";
        }
    }

    private async void BrowseInputFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
            ViewModel.InputFolder = path;
    }

    private async void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
            ViewModel.OutputFolder = path;
    }

    private static async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
