namespace MediaMatch.Core.Models;

/// <summary>
/// Describes a subtitle file available for download or local use.
/// </summary>
/// <param name="Name">The display name of the subtitle.</param>
/// <param name="Language">The ISO language code of the subtitle.</param>
/// <param name="Format">The subtitle file format.</param>
/// <param name="ProviderName">The name of the provider that supplied the subtitle.</param>
/// <param name="DownloadUrl">The URL to download the subtitle file.</param>
/// <param name="Hash">The hash of the subtitle file for deduplication.</param>
/// <param name="Downloads">The number of times this subtitle has been downloaded.</param>
public sealed record SubtitleDescriptor(
    string Name,
    string Language,
    SubtitleFormat Format,
    string? ProviderName = null,
    string? DownloadUrl = null,
    string? Hash = null,
    int? Downloads = null);

/// <summary>
/// Specifies the file format of a subtitle.
/// </summary>
public enum SubtitleFormat
{
    /// <summary>SubRip (.srt) format.</summary>
    SubRip,

    /// <summary>Sub Station Alpha (.ssa/.ass) format.</summary>
    SubStationAlpha,

    /// <summary>SubViewer (.sub) format.</summary>
    SubViewer,

    /// <summary>MicroDVD (.sub) frame-based format.</summary>
    MicroDVD,

    /// <summary>SAMI (.smi) format.</summary>
    Sami,

    /// <summary>Unknown or unrecognized subtitle format.</summary>
    Unknown
}
