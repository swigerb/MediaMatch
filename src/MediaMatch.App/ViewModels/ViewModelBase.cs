using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// Base class for all view models. Extends ObservableObject from CommunityToolkit.Mvvm
/// to provide INotifyPropertyChanged/INotifyPropertyChanging support.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
}
