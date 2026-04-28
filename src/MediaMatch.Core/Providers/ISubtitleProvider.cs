using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

/// <summary>
/// Provides subtitle search and download capabilities.
/// </summary>
public interface ISubtitleProvider
{
    /// <summary>Gets the display name of this provider.</summary>
    string Name { get; }

    /// <summary>
    /// Searches for subtitles matching the specified query and language.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="language">The desired subtitle language code.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of matching subtitle descriptors.</returns>
    Task<IReadOnlyList<SubtitleDescriptor>> SearchAsync(string query, string language, CancellationToken ct = default);

    /// <summary>
    /// Searches for subtitles by file hash and size.
    /// </summary>
    /// <param name="movieHash">The hash of the video file.</param>
    /// <param name="fileSize">The size of the video file in bytes.</param>
    /// <param name="language">The desired subtitle language code.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of matching subtitle descriptors.</returns>
    Task<IReadOnlyList<SubtitleDescriptor>> SearchByHashAsync(string movieHash, long fileSize, string language, CancellationToken ct = default);

    /// <summary>
    /// Downloads the specified subtitle file.
    /// </summary>
    /// <param name="subtitle">The subtitle descriptor to download.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A stream containing the subtitle file content.</returns>
    Task<Stream> DownloadAsync(SubtitleDescriptor subtitle, CancellationToken ct = default);
}
