namespace MediaMatch.Core.Models;

/// <summary>
/// Represents technical metadata extracted from a video file via ffprobe or container header analysis.
/// </summary>
/// <param name="AudioChannels">The audio channel layout (e.g., "5.1 Surround").</param>
/// <param name="DolbyVision">The Dolby Vision profile, or <c>null</c> if not present.</param>
/// <param name="HdrFormat">The HDR format (e.g., "HDR10", "HDR10+"), or <c>null</c> if SDR.</param>
/// <param name="Resolution">The video resolution label (e.g., "1080p", "4K").</param>
/// <param name="BitDepth">The video bit depth (e.g., "8bit", "10bit").</param>
/// <param name="VideoCodec">The video codec name (e.g., "HEVC", "AV1").</param>
/// <param name="AudioCodec">The audio codec name (e.g., "AAC", "TrueHD Atmos").</param>
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
