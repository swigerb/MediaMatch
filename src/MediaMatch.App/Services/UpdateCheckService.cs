using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;

namespace MediaMatch.App.Services;

/// <summary>
/// Checks for application updates using Velopack.
/// Currently a stub implementation — Velopack integration will be wired
/// once the NuGet package is confirmed available for .NET 10 + WinUI 3.
/// The interface (<see cref="IUpdateCheckService"/>) is ready for binding.
/// </summary>
public sealed class UpdateCheckService : IUpdateCheckService
{
    private readonly ILogger<UpdateCheckService> _logger;

    /// <inheritdoc />
    public bool IsUpdateAvailable { get; private set; }

    /// <inheritdoc />
    public string? LatestVersion { get; private set; }

    /// <inheritdoc />
    public string? ReleaseNotes { get; private set; }

    public UpdateCheckService(ILogger<UpdateCheckService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Checking for application updates...");

            // TODO: Replace with Velopack.UpdateManager when package is available
            // var mgr = new UpdateManager("https://github.com/swigerb/MediaMatch/releases");
            // var updateInfo = await mgr.CheckForUpdatesAsync();
            // if (updateInfo is not null)
            // {
            //     IsUpdateAvailable = true;
            //     LatestVersion = updateInfo.TargetFullRelease.Version.ToString();
            //     ReleaseNotes = updateInfo.TargetFullRelease.Body;
            // }

            // Stub: simulate no update available
            await Task.Delay(100, ct); // Simulate network call
            IsUpdateAvailable = false;
            LatestVersion = null;
            ReleaseNotes = null;

            _logger.LogInformation("Update check complete. Available: {IsAvailable}", IsUpdateAvailable);
            return IsUpdateAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed — continuing without update");
            IsUpdateAvailable = false;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DownloadAndApplyAsync(CancellationToken ct = default)
    {
        if (!IsUpdateAvailable)
        {
            _logger.LogWarning("No update available to apply");
            return;
        }

        try
        {
            _logger.LogInformation("Downloading and applying update to v{Version}...", LatestVersion);

            // TODO: Replace with Velopack.UpdateManager when package is available
            // var mgr = new UpdateManager("https://github.com/swigerb/MediaMatch/releases");
            // var updateInfo = await mgr.CheckForUpdatesAsync();
            // await mgr.DownloadUpdatesAsync(updateInfo);
            // mgr.ApplyUpdatesAndRestart(updateInfo);

            await Task.CompletedTask;
            _logger.LogWarning("Velopack not yet integrated — update download is a no-op");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download and apply update");
            throw;
        }
    }
}
