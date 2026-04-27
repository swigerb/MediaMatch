namespace MediaMatch.Core.Providers;

public interface IMediaAnalyzer
{
    string Name { get; }

    Task<MediaAnalysis> AnalyzeAsync(string filePath, CancellationToken ct = default);
}

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
