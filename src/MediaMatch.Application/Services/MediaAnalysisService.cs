using MediaMatch.Application.Detection;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;

namespace MediaMatch.Application.Services;

/// <summary>
/// Analyzes file paths to extract media metadata by combining
/// filename parsing with directory structure analysis.
/// </summary>
public sealed class MediaAnalysisService : IMediaAnalysisService
{
    private readonly MediaDetector _detector;
    private readonly ReleaseInfoParser _releaseParser;

    public MediaAnalysisService()
    {
        _releaseParser = new ReleaseInfoParser();
        _detector = new MediaDetector(_releaseParser);
    }

    public MediaAnalysisService(MediaDetector detector, ReleaseInfoParser releaseParser)
    {
        _detector = detector;
        _releaseParser = releaseParser;
    }

    public Task<MediaAnalysisResult> AnalyzeAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ct.ThrowIfCancellationRequested();

        var result = AnalyzeFile(filePath);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<MediaAnalysisResult>> AnalyzeBatchAsync(
        IReadOnlyList<string> filePaths,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        var results = new MediaAnalysisResult[filePaths.Count];
        for (int i = 0; i < filePaths.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            results[i] = AnalyzeFile(filePaths[i]);
        }

        return Task.FromResult<IReadOnlyList<MediaAnalysisResult>>(results);
    }

    private MediaAnalysisResult AnalyzeFile(string filePath)
    {
        var detection = _detector.Detect(filePath);
        var releaseInfo = detection.ReleaseInfo;

        // Augment with directory structure analysis
        var dirTitle = ExtractTitleFromDirectory(filePath, detection.MediaType);
        var cleanTitle = !string.IsNullOrWhiteSpace(releaseInfo.CleanTitle)
            ? releaseInfo.CleanTitle
            : dirTitle;

        return new MediaAnalysisResult(
            FilePath: filePath,
            MediaType: detection.MediaType,
            Confidence: detection.Confidence,
            CleanTitle: cleanTitle,
            Season: releaseInfo.SeasonEpisode?.Season,
            Episode: releaseInfo.SeasonEpisode?.Episode,
            Year: releaseInfo.Year,
            VideoQuality: releaseInfo.Quality != VideoQuality.Unknown ? releaseInfo.Quality.ToString() : null,
            ReleaseGroup: releaseInfo.ReleaseGroup,
            VideoSource: releaseInfo.VideoSource);
    }

    /// <summary>
    /// Extract additional title information from directory structure.
    /// Common patterns: /TV Shows/Breaking Bad/Season 01/file.mkv
    /// </summary>
    private static string? ExtractTitleFromDirectory(string filePath, MediaType mediaType)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir))
                return null;

            var dirName = Path.GetFileName(dir);
            if (string.IsNullOrEmpty(dirName))
                return null;

            // If directory looks like "Season XX", go up one more level
            if (dirName.StartsWith("Season", StringComparison.OrdinalIgnoreCase) ||
                dirName.StartsWith("Series", StringComparison.OrdinalIgnoreCase) ||
                dirName.StartsWith("Specials", StringComparison.OrdinalIgnoreCase))
            {
                var parentDir = Path.GetDirectoryName(dir);
                if (!string.IsNullOrEmpty(parentDir))
                    return Path.GetFileName(parentDir);
            }

            return dirName;
        }
        catch
        {
            return null;
        }
    }
}
