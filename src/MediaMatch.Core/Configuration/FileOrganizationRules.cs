using MediaMatch.Core.Enums;

namespace MediaMatch.Core.Configuration;

/// <summary>
/// Rules that govern where media files are placed after matching.
/// Each rule maps a media type to a folder structure pattern.
/// </summary>
public sealed class FileOrganizationRules
{
    /// <summary>Organization pattern for movies. Default: Movies/{title} ({year})</summary>
    public string MovieRule { get; set; } = "Movies/{Name} ({Year})";

    /// <summary>Organization pattern for TV series. Default: Shows/{series}/Season {n}</summary>
    public string SeriesRule { get; set; } = "Shows/{SeriesName}/Season {Season}";

    /// <summary>Organization pattern for anime. Default: Anime/{series}/Season {n}</summary>
    public string AnimeRule { get; set; } = "Anime/{SeriesName}/Season {Season}";

    /// <summary>
    /// Returns the organization rule pattern for the given media type, or <c>null</c>
    /// if no organization pattern is defined for that type.
    /// </summary>
    /// <param name="mediaType">The media type to look up.</param>
    /// <returns>
    /// The matching organization rule pattern for <see cref="MediaType.Movie"/>,
    /// <see cref="MediaType.TvSeries"/>, or <see cref="MediaType.Anime"/>.
    /// Returns <c>null</c> for <see cref="MediaType.Music"/>, <see cref="MediaType.Subtitle"/>,
    /// and <see cref="MediaType.Unknown"/>, which have no defined folder structure.
    /// </returns>
    public string? GetRuleForMediaType(MediaType mediaType) => mediaType switch
    {
        MediaType.Movie => MovieRule,
        MediaType.TvSeries => SeriesRule,
        MediaType.Anime => AnimeRule,
        MediaType.Music => null,
        MediaType.Subtitle => null,
        MediaType.Unknown => null,
        _ => null
    };
}
