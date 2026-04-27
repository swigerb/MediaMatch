namespace MediaMatch.Core.Models;

/// <summary>
/// Technical metadata extracted from a video file via ffprobe or container header analysis.
/// </summary>
public sealed record MediaTechnicalInfo(
    string AudioChannels,
    string? DolbyVision,
    string? HdrFormat,
    string Resolution,
    string BitDepth,
    string VideoCodec,
    string AudioCodec)
{
    /// <summary>Returns a default/unknown instance when extraction fails.</summary>
    public static MediaTechnicalInfo Unknown => new(
        AudioChannels: "2.0 Stereo",
        DolbyVision: null,
        HdrFormat: null,
        Resolution: "SD",
        BitDepth: "8bit",
        VideoCodec: "Unknown",
        AudioCodec: "Unknown");
}
