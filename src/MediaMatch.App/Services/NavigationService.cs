using MediaMatch.App.Pages;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.Services;

/// <summary>
/// Concrete navigation service backed by a WinUI Frame.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private Frame? _frame;

    private static readonly Dictionary<string, Type> PageMap = new()
    {
        ["home"] = typeof(HomePage),
        ["history"] = typeof(HistoryPage),
        ["settings"] = typeof(SettingsPage),
        ["about"] = typeof(AboutPage),
    };

    public void SetFrame(Frame frame) => _frame = frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void NavigateTo(string pageKey)
    {
        if (PageMap.TryGetValue(pageKey, out var pageType))
        {
            _frame?.Navigate(pageType);
        }
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
        }
    }
}
