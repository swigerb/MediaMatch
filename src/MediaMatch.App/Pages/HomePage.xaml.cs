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
}
