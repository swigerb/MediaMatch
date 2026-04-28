namespace MediaMatch.Core.Models;

/// <summary>
/// Represents audio metadata extracted from an audio file's embedded tags.
/// </summary>
/// <param name="Artist">The track artist name.</param>
/// <param name="Title">The track title.</param>
/// <param name="Album">The album title.</param>
/// <param name="AlbumArtist">The album-level artist name.</param>
/// <param name="Track">The track number within the disc.</param>
/// <param name="Disc">The disc number within a multi-disc release.</param>
/// <param name="Genre">The genre classification.</param>
/// <param name="Year">The release year as a string.</param>
/// <param name="AcoustIdFingerprint">The AcoustID audio fingerprint for identification.</param>
public sealed record AudioTrack(
    string? Artist = null,
    string? Title = null,
    string? Album = null,
    string? AlbumArtist = null,
    int? Track = null,
    int? Disc = null,
    string? Genre = null,
    string? Year = null,
    string? AcoustIdFingerprint = null);
