namespace MediaMatch.App.Services;

/// <summary>
/// Abstraction for page navigation, keeping ViewModels free of UI dependencies.
/// </summary>
public interface INavigationService
{
    void NavigateTo(string pageKey);
    bool CanGoBack { get; }
    void GoBack();
}
