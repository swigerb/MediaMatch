namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a piece of artwork (poster, banner, fanart, etc.) for a media item.
/// </summary>
/// <param name="Url">The URL of the artwork image.</param>
/// <param name="Type">The artwork category.</param>
/// <param name="Language">The language code of the artwork, or <c>null</c> if language-neutral.</param>
/// <param name="Rating">The community rating of the artwork.</param>
/// <param name="Width">The image width in pixels.</param>
/// <param name="Height">The image height in pixels.</param>
public sealed record Artwork(
    string Url,
    ArtworkType Type,
    string? Language = null,
    double? Rating = null,
    int? Width = null,
    int? Height = null);

/// <summary>
/// Specifies the category of artwork associated with a media item.
/// </summary>
public enum ArtworkType
{
    /// <summary>A portrait-oriented promotional poster.</summary>
    Poster,

    /// <summary>A wide banner image, typically used for series.</summary>
    Banner,

    /// <summary>A background or fan-created artwork image.</summary>
    Fanart,

    /// <summary>A transparent artwork overlay with the title rendered artistically.</summary>
    Clearart,

    /// <summary>A transparent logo of the media title.</summary>
    Clearlogo,

    /// <summary>A landscape-oriented 16:9 promotional image.</summary>
    Landscape,

    /// <summary>A season-specific poster image.</summary>
    Season,

    /// <summary>A thumbnail-sized preview image.</summary>
    Thumb
}
