namespace MediaMatch.Core.Models;

/// <summary>
/// Identifies the source provider for a subtitle search result.
/// </summary>
public enum SubtitleSource
{
    /// <summary>Subtitle from the OpenSubtitles online database.</summary>
    OpenSubtitles,

    /// <summary>Subtitle from the SubDB hash-based database.</summary>
    SubDB,

    /// <summary>Subtitle from a local file alongside the media.</summary>
    Local
}
