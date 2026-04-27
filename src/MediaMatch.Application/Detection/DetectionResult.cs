using MediaMatch.Core.Enums;

namespace MediaMatch.Application.Detection;

public sealed record DetectionResult(
    string FilePath,
    MediaType MediaType,
    ReleaseInfo ReleaseInfo,
    float Confidence);
