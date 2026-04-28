using MediaMatch.Core.Enums;

namespace MediaMatch.Application.Detection;

/// <summary>
/// Represents the result of media type detection for a file.
/// </summary>
/// <param name="FilePath">Gets the full path to the detected file.</param>
/// <param name="MediaType">Gets the detected media type.</param>
/// <param name="ReleaseInfo">Gets the parsed release metadata.</param>
/// <param name="Confidence">Gets the detection confidence score between 0 and 1.</param>
public sealed record DetectionResult(
    string FilePath,
    MediaType MediaType,
    ReleaseInfo ReleaseInfo,
    float Confidence);
