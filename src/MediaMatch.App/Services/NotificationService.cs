using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace MediaMatch.App.Services;

/// <summary>
/// Provides non-modal InfoBar notifications at the top of pages.
/// Auto-dismisses after 5 seconds. Supports success, error, and info severity.
/// </summary>
public sealed class NotificationService
{
    private InfoBar? _infoBar;
    private DispatcherTimer? _dismissTimer;

    /// <summary>
    /// Binds the service to an InfoBar control on the page.
    /// Call this from code-behind after InitializeComponent.
    /// </summary>
    public void SetInfoBar(InfoBar infoBar)
    {
        _infoBar = infoBar;
    }

    public void ShowSuccess(string message)
        => Show(message, InfoBarSeverity.Success);

    public void ShowError(string message)
        => Show(message, InfoBarSeverity.Error);

    public void ShowWarning(string message)
        => Show(message, InfoBarSeverity.Warning);

    public void ShowInfo(string message)
        => Show(message, InfoBarSeverity.Informational);

    private void Show(string message, InfoBarSeverity severity)
    {
        if (_infoBar is null) return;

        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            _infoBar.Message = message;
            _infoBar.Severity = severity;
            _infoBar.IsOpen = true;

            _dismissTimer?.Stop();
            _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _dismissTimer.Tick += (_, _) =>
            {
                _infoBar.IsOpen = false;
                _dismissTimer.Stop();
            };
            _dismissTimer.Start();
        });
    }
}
