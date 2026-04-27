namespace MediaMatch.Core.Services;

/// <summary>
/// Checks for application updates and applies them.
/// </summary>
public interface IUpdateCheckService
{
    /// <summary>Whether an update is available after the last check.</summary>
    bool IsUpdateAvailable { get; }

    /// <summary>Latest version string (e.g. "0.2.0"), or null if not checked.</summary>
    string? LatestVersion { get; }

    /// <summary>Release notes for the latest version, if available.</summary>
    string? ReleaseNotes { get; }

    /// <summary>
    /// Checks for updates asynchronously. Returns true if an update is available.
    /// </summary>
    Task<bool> CheckForUpdatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Downloads and applies the available update, then restarts the application.
    /// </summary>
    Task DownloadAndApplyAsync(CancellationToken ct = default);
}
