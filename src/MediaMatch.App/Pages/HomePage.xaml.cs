using MediaMatch.App.Dialogs;
using MediaMatch.App.Services;
using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace MediaMatch.App.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();

        // Wire the notification InfoBar to the NotificationService
        var notificationService = App.GetService<NotificationService>();
        notificationService.SetInfoBar(NotificationBar);
        ViewModel.SetNotificationService(notificationService);

        // Rebuild the presets flyout whenever the collection changes
        ViewModel.Presets.CollectionChanged += (_, _) => RebuildPresetsFlyout();
        RebuildPresetsFlyout();
    }

    private void RebuildPresetsFlyout()
    {
        PresetsFlyout.Items.Clear();

        foreach (var preset in ViewModel.Presets)
        {
            var item = new MenuFlyoutItem
            {
                Text = preset.Name,
                Tag = preset,
                Icon = new FontIcon { Glyph = "\uE762" }
            };
            item.Click += (s, _) =>
            {
                if (s is MenuFlyoutItem { Tag: Core.Configuration.PresetDefinitionSettings p })
                    ViewModel.ApplyPreset(p);
            };
            PresetsFlyout.Items.Add(item);
        }

        if (ViewModel.Presets.Count > 0)
            PresetsFlyout.Items.Add(new MenuFlyoutSeparator());

        var editItem = new MenuFlyoutItem
        {
            Text = "Edit Presets\u2026",
            Icon = new FontIcon { Glyph = "\uE70F" }
        };
        editItem.Click += async (_, _) =>
        {
            if (ViewModel.EditPresetsCommand.CanExecute(null))
                await ViewModel.EditPresetsCommand.ExecuteAsync(null);
        };
        PresetsFlyout.Items.Add(editItem);
    }

    private void Page_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Add files";
        e.DragUIOverride.IsCaptionVisible = true;
    }

    private async void Page_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        var paths = items
            .Select(item => item.Path)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (paths.Count > 0)
        {
            ViewModel.AddFiles(paths);
        }
    }

    private void SourceFiles_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop to add files";
        e.DragUIOverride.IsCaptionVisible = true;
        e.Handled = true;

        // Visual highlight on the border
        SourceFilesBorder.BorderThickness = new Thickness(2);
        SourceFilesBorder.Background = (Microsoft.UI.Xaml.Media.Brush)Resources["ControlFillColorDefaultBrush"]
            ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private void SourceFiles_DragLeave(object sender, DragEventArgs e)
    {
        SourceFilesBorder.BorderThickness = new Thickness(1);
        SourceFilesBorder.Background = null;
    }

    private async void SourceFiles_Drop(object sender, DragEventArgs e)
    {
        // Reset visual state
        SourceFilesBorder.BorderThickness = new Thickness(1);
        SourceFilesBorder.Background = null;
        e.Handled = true;

        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        foreach (var item in items)
        {
            if (item is Windows.Storage.StorageFolder folder)
            {
                // Dropped a folder — scan it
                await ViewModel.ScanDroppedFolderAsync(folder.Path);
            }
            else if (!string.IsNullOrEmpty(item.Path))
            {
                ViewModel.AddFiles(new[] { item.Path });
            }
        }
    }

    private void EmptyState_Tapped(object sender, TappedRoutedEventArgs e)
    {
        EmptyStateLoadFlyout.ShowAt(EmptyStateArea);
    }

    private void ModeSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        // Show/hide panels based on selected mode
        RenamePanel.Visibility = sender.SelectedItem == RenameMode ? Visibility.Visible : Visibility.Collapsed;
        EpisodesPanel.Visibility = sender.SelectedItem == EpisodesMode ? Visibility.Visible : Visibility.Collapsed;
        SubtitlesPanel.Visibility = sender.SelectedItem == SubtitlesMode ? Visibility.Visible : Visibility.Collapsed;
        SfvPanel.Visibility = sender.SelectedItem == SfvMode ? Visibility.Visible : Visibility.Collapsed;
        FilterPanel.Visibility = sender.SelectedItem == FilterMode ? Visibility.Visible : Visibility.Collapsed;
        ListPanel.Visibility = sender.SelectedItem == ListMode ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnKeyboardAcceleratorInvoked(KeyboardAcceleratorInvokedEventArgs args)
    {
        var accel = args.KeyboardAccelerator;
        switch (accel.Key)
        {
            case VirtualKey.O when accel.Modifiers == VirtualKeyModifiers.Control:
                if (ViewModel.AddFolderCommand.CanExecute(null))
                    ViewModel.AddFolderCommand.Execute(null);
                args.Handled = true;
                break;

            case VirtualKey.A when accel.Modifiers == VirtualKeyModifiers.Control:
                if (ViewModel.SelectAllCommand.CanExecute(null))
                    ViewModel.SelectAllCommand.Execute(null);
                args.Handled = true;
                break;

            case VirtualKey.Delete:
                if (ViewModel.RemoveSelectedCommand.CanExecute(null))
                    ViewModel.RemoveSelectedCommand.Execute(null);
                args.Handled = true;
                break;

            case VirtualKey.Z when accel.Modifiers == VirtualKeyModifiers.Control:
                if (ViewModel.UndoLastCommand.CanExecute(null))
                    ViewModel.UndoLastCommand.Execute(null);
                args.Handled = true;
                break;

            case VirtualKey.F5:
                if (ViewModel.RefreshCommand.CanExecute(null))
                    ViewModel.RefreshCommand.Execute(null);
                args.Handled = true;
                break;

            case VirtualKey.F1:
                _ = ShowKeyboardShortcutsAsync();
                args.Handled = true;
                break;
        }

        base.OnKeyboardAcceleratorInvoked(args);
    }

    private async Task ShowKeyboardShortcutsAsync()
    {
        var dialog = new KeyboardShortcutsDialog
        {
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async void MediaInfoInspector_Click(object sender, RoutedEventArgs e)
    {
        await ShowMediaInfoInspectorAsync(filePath: null);
    }

    private async Task ShowMediaInfoInspectorAsync(string? filePath)
    {
        var vm = App.GetService<MediaInfoInspectorViewModel>();
        var dialog = new MediaInfoInspectorDialog(vm, filePath)
        {
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
