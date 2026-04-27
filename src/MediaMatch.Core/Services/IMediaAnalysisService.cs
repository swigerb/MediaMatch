using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;

namespace MediaMatch.Core.Services;

/// <summary>
/// Analyzes a file path to extract media metadata from filename and directory structure.
/// </summary>
public interface IMediaAnalysisService
{
    /// <summary>
    /// Analyze a single file to detect media type and extract metadata.
    /// </summary>
    Task<MediaAnalysisResult> AnalyzeAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Analyze a batch of files.
    /// </summary>
    Task<IReadOnlyList<MediaAnalysisResult>> AnalyzeBatchAsync(
        IReadOnlyList<string> filePaths,
        CancellationToken ct = default);
}

/// <summary>
/// Combined analysis result from filename parsing and directory structure analysis.
/// </summary>
public sealed record MediaAnalysisResult(
    string FilePath,
    MediaType MediaType,
    float Confidence,
    string? CleanTitle,
    int? Season,
    int? Episode,
    int? Year,
    string? VideoQuality,
    string? ReleaseGroup,
    string? VideoSource);
