namespace MediaMatch.Core.Enums;

/// <summary>
/// Specifies the video resolution quality tier.
/// </summary>
public enum VideoQuality
{
    /// <summary>Unknown or undetected resolution.</summary>
    Unknown,

    /// <summary>Standard definition (below 720p).</summary>
    SD,

    /// <summary>720p high definition (1280×720).</summary>
    HD720p,

    /// <summary>1080p full high definition (1920×1080).</summary>
    HD1080p,

    /// <summary>4K ultra high definition (3840×2160).</summary>
    UHD4K,

    /// <summary>8K ultra high definition (7680×4320).</summary>
    UHD8K
}
