using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

/// <summary>
/// Provider for music track metadata via fingerprint or artist/title search.
/// </summary>
public interface IMusicProvider
{
    string Name { get; }

    /// <summary>
    /// Lookup a track by audio fingerprint and duration.
    /// </summary>
    Task<MusicTrack?> LookupAsync(string fingerprint, int duration, CancellationToken ct = default);

    /// <summary>
    /// Search for tracks by artist and title.
    /// </summary>
    Task<IReadOnlyList<MusicTrack>> SearchAsync(string artist, string title, CancellationToken ct = default);
}
