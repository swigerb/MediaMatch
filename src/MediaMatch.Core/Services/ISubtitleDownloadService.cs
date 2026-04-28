using MediaMatch.Core.Models;

namespace MediaMatch.Core.Services;

/// <summary>
/// Downloads subtitle files and saves them alongside the video file.
/// </summary>
public interface ISubtitleDownloadService
{
    /// <summary>
    /// Downloads a subtitle and saves it alongside the specified video file.
    /// Returns the full path to the saved subtitle file.
    /// </summary>
    /// <param name="subtitle">The subtitle descriptor to download.</param>
    /// <param name="videoFilePath">The path to the video file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The full path to the saved subtitle file.</returns>
    Task<string> DownloadAndSaveAsync(
        SubtitleDescriptor subtitle,
        string videoFilePath,
        CancellationToken ct = default);
}
