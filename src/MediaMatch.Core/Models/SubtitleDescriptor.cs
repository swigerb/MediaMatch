namespace MediaMatch.Core.Models;

public sealed record SubtitleDescriptor(
    string Name,
    string Language,
    SubtitleFormat Format,
    string? ProviderName = null,
    string? DownloadUrl = null,
    string? Hash = null,
    int? Downloads = null);

public enum SubtitleFormat
{
    SubRip,
    SubStationAlpha,
    SubViewer,
    MicroDVD,
    Sami,
    Unknown
}
