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
    /// Returns the organization rule pattern for the given media type string.
    /// </summary>
    public string GetRuleForMediaType(string mediaType) => mediaType.ToUpperInvariant() switch
    {
        "MOVIE" => MovieRule,
        "SERIES" or "TV" => SeriesRule,
        "ANIME" => AnimeRule,
        _ => SeriesRule
    };
}
