using MediaMatch.Core.Models;

namespace MediaMatch.Core.Services;

/// <summary>
/// Extracts complete raw media information from a file (all streams, all properties).
/// Unlike <see cref="IMediaAnalysisService"/> which parses filenames, this service
/// reads actual container metadata via ffprobe/mediainfo for full property access.
/// </summary>
public interface IMediaInfoService
{
    /// <summary>
    /// Extracts all media properties from a file.
    /// Returns null if the file cannot be probed (not a media file, ffprobe unavailable, etc.).
    /// </summary>
    Task<MediaInfoResult?> GetMediaInfoAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Checks whether the media info backend (ffprobe or mediainfo) is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
