using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

/// <summary>
/// Provider for music track metadata via fingerprint or artist/title search.
/// </summary>
public interface IMusicProvider
{
    /// <summary>Gets the display name of this provider.</summary>
    string Name { get; }

    /// <summary>
    /// Lookup a track by audio fingerprint and duration.
    /// </summary>
    /// <param name="fingerprint">The audio fingerprint string.</param>
    /// <param name="duration">The track duration in seconds.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The matching music track, or <see langword="null"/> if not found.</returns>
    Task<MusicTrack?> LookupAsync(string fingerprint, int duration, CancellationToken ct = default);

    /// <summary>
    /// Search for tracks by artist and title.
    /// </summary>
    /// <param name="artist">The artist name.</param>
    /// <param name="title">The track title.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of matching music tracks.</returns>
    Task<IReadOnlyList<MusicTrack>> SearchAsync(string artist, string title, CancellationToken ct = default);
}
