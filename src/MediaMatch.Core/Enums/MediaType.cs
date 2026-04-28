namespace MediaMatch.Core.Enums;

/// <summary>
/// Specifies the type of media content.
/// </summary>
public enum MediaType
{
    /// <summary>A feature film.</summary>
    Movie,

    /// <summary>A television series.</summary>
    TvSeries,

    /// <summary>An anime series.</summary>
    Anime,

    /// <summary>A music track or album.</summary>
    Music,

    /// <summary>A subtitle file.</summary>
    Subtitle,

    /// <summary>An unrecognized or unclassified media type.</summary>
    Unknown
}
