namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a music track with metadata from MusicBrainz, AcoustID, or embedded tags.
/// </summary>
public sealed record MusicTrack(
    string Title,
    string Artist,
    string? Album = null,
    string? AlbumArtist = null,
    int? TrackNumber = null,
    int? DiscNumber = null,
    int? TotalDiscs = null,
    string? Genre = null,
    int? Year = null,
    List<string>? FeaturedArtists = null,
    string? MusicBrainzId = null,
    int? Duration = null)
{
    /// <summary>Display-friendly artist including featured artists.</summary>
    public string DisplayArtist =>
        FeaturedArtists is { Count: > 0 }
            ? $"{Artist} feat. {string.Join(", ", FeaturedArtists)}"
            : Artist;
}
