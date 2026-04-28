using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;

namespace MediaMatch.App.ViewModels;

/// <summary>
/// ViewModel for update notifications. Binds to UpdateCheckService
/// and exposes commands for one-click update + restart.
/// </summary>
public sealed partial class UpdateViewModel : ViewModelBase
{
    private readonly IUpdateCheckService _updateService;
    private readonly ILogger<UpdateViewModel> _logger;

    /// <summary>Gets or sets a value indicating whether an update is available.</summary>
    [ObservableProperty]
    public partial bool IsUpdateAvailable { get; set; }

    /// <summary>Gets or sets the latest available version string.</summary>
    [ObservableProperty]
    public partial string? LatestVersion { get; set; }

    /// <summary>Gets or sets the release notes for the latest version.</summary>
    [ObservableProperty]
    public partial string? ReleaseNotes { get; set; }

    /// <summary>Gets or sets a value indicating whether an update check is in progress.</summary>
    [ObservableProperty]
    public partial bool IsChecking { get; set; }

    /// <summary>Gets or sets a value indicating whether an update is being applied.</summary>
    [ObservableProperty]
    public partial bool IsApplying { get; set; }

    /// <summary>Gets or sets the status message for update operations.</summary>
    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateViewModel"/> class.
    /// </summary>
    /// <param name="updateService">The update check service.</param>
    /// <param name="logger">The logger instance.</param>
    public UpdateViewModel(IUpdateCheckService updateService, ILogger<UpdateViewModel> logger)
    {
        _updateService = updateService;
        _logger = logger;
    }

    /// <summary>
    /// Checks for updates. Called automatically on app startup (fire-and-forget)
    /// or manually from the UI.
    /// </summary>
    [RelayCommand]
    private async Task CheckForUpdatesAsync(CancellationToken ct)
    {
        if (IsChecking) return;

        IsChecking = true;
        StatusMessage = "Checking for updates…";

        try
        {
            var available = await _updateService.CheckForUpdatesAsync(ct);

            IsUpdateAvailable = available;
            LatestVersion = _updateService.LatestVersion;
            ReleaseNotes = _updateService.ReleaseNotes;

            StatusMessage = available
                ? $"Update v{LatestVersion} available!"
                : "You're up to date.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
            StatusMessage = "Could not check for updates.";
        }
        finally
        {
            IsChecking = false;
        }
    }

    /// <summary>
    /// Downloads and applies the update, then restarts the application.
    /// </summary>
    [RelayCommand]
    private async Task ApplyUpdateAsync(CancellationToken ct)
    {
        if (!IsUpdateAvailable || IsApplying) return;

        IsApplying = true;
        StatusMessage = $"Downloading v{LatestVersion}…";

        try
        {
            await _updateService.DownloadAndApplyAsync(ct);
            StatusMessage = "Update applied — restarting…";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Update cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply update");
            StatusMessage = "Update failed. Please try again later.";
        }
        finally
        {
            IsApplying = false;
        }
    }
}
