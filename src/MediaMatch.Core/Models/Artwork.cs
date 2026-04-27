namespace MediaMatch.Core.Models;

public sealed record Artwork(
    string Url,
    ArtworkType Type,
    string? Language = null,
    double? Rating = null,
    int? Width = null,
    int? Height = null);

public enum ArtworkType
{
    Poster,
    Banner,
    Fanart,
    Clearart,
    Clearlogo,
    Landscape,
    Season,
    Thumb
}
