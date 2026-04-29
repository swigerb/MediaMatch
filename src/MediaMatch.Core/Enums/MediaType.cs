namespace MediaMatch.Core.Enums;

/// <summary>
/// Specifies the type of media content.
/// </summary>
public enum MediaType
{
    /// <summary>An unrecognized or unclassified media type. This is the default value.</summary>
    Unknown = 0,

    /// <summary>A feature film.</summary>
    Movie = 1,

    /// <summary>A television series.</summary>
    TvSeries = 2,

    /// <summary>An anime series.</summary>
    Anime = 3,

    /// <summary>A music track or album.</summary>
    Music = 4,

    /// <summary>A subtitle file.</summary>
    Subtitle = 5
}
