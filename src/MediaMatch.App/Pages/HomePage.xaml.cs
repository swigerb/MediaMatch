using MediaMatch.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace MediaMatch.App.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
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
        }

        base.OnKeyboardAcceleratorInvoked(args);
    }
}
