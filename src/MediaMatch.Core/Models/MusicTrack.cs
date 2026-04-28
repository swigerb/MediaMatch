namespace MediaMatch.Core.Models;

/// <summary>
/// Represents a music track with metadata from MusicBrainz, AcoustID, or embedded tags.
/// </summary>
/// <param name="Title">The track title.</param>
/// <param name="Artist">The primary artist name.</param>
/// <param name="Album">The album title.</param>
/// <param name="AlbumArtist">The album-level artist name.</param>
/// <param name="TrackNumber">The track number within the disc.</param>
/// <param name="DiscNumber">The disc number within a multi-disc release.</param>
/// <param name="TotalDiscs">The total number of discs in the release.</param>
/// <param name="Genre">The genre classification.</param>
/// <param name="Year">The release year.</param>
/// <param name="FeaturedArtists">The list of featured artists on the track.</param>
/// <param name="MusicBrainzId">The MusicBrainz recording identifier.</param>
/// <param name="Duration">The track duration in seconds.</param>
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
