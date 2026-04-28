using MediaMatch.Core.Enums;

namespace MediaMatch.Application.Detection;

/// <summary>
/// Represents metadata extracted from a media release filename.
/// </summary>
/// <param name="OriginalFileName">Gets the original filename before parsing.</param>
/// <param name="CleanTitle">Gets the cleaned title with release noise removed.</param>
/// <param name="SeasonEpisode">Gets the season and episode match, if detected.</param>
/// <param name="Year">Gets the release year, if detected.</param>
/// <param name="Quality">Gets the detected video quality.</param>
/// <param name="VideoSource">Gets the video source (e.g., BluRay, WEB-DL), if detected.</param>
/// <param name="VideoCodec">Gets the video codec (e.g., H.265, AV1), if detected.</param>
/// <param name="AudioCodec">Gets the audio codec (e.g., TrueHD, DTS), if detected.</param>
/// <param name="ReleaseGroup">Gets the release group name, if detected.</param>
/// <param name="Language">Gets the language tag, if detected.</param>
/// <param name="HdrFormat">Gets the HDR format (e.g., HDR10, HDR10+), if detected.</param>
/// <param name="DolbyVision">Gets the Dolby Vision profile, if detected.</param>
/// <param name="AudioChannels">Gets the audio channel layout (e.g., 5.1, 7.1), if detected.</param>
/// <param name="BitDepth">Gets the bit depth (e.g., 10bit), if detected.</param>
public sealed record ReleaseInfo(
    string OriginalFileName,
    string CleanTitle,
    SeasonEpisodeMatch? SeasonEpisode,
    int? Year,
    VideoQuality Quality,
    string? VideoSource,
    string? VideoCodec,
    string? AudioCodec,
    string? ReleaseGroup,
    string? Language,
    string? HdrFormat = null,
    string? DolbyVision = null,
    string? AudioChannels = null,
    string? BitDepth = null);
