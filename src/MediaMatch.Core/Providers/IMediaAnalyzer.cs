namespace MediaMatch.Core.Providers;

/// <summary>
/// Analyzes media files to extract technical properties such as codecs, resolution, and duration.
/// </summary>
public interface IMediaAnalyzer
{
    /// <summary>Gets the display name of this analyzer.</summary>
    string Name { get; }

    /// <summary>
    /// Analyzes the specified media file and extracts its technical properties.
    /// </summary>
    /// <param name="filePath">The path to the media file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The analysis result containing the file's technical properties.</returns>
    Task<MediaAnalysis> AnalyzeAsync(string filePath, CancellationToken ct = default);
}

/// <summary>
/// Contains technical properties extracted from a media file.
/// </summary>
public sealed record MediaAnalysis(
    string FilePath,
    TimeSpan? Duration,
    string? VideoCodec,
    string? AudioCodec,
    int? Width,
    int? Height,
    double? FrameRate,
    long? FileSize,
    string? Container,
    int? AudioChannels = null,
    int? BitRate = null,
    string? VideoProfile = null);
