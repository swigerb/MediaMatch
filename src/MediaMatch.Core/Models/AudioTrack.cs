namespace MediaMatch.Core.Models;

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
