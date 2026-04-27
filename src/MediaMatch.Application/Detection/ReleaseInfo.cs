using MediaMatch.Core.Enums;

namespace MediaMatch.Application.Detection;

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
    string? Language);
