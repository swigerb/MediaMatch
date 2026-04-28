using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

/// <summary>
/// Marker interface for metadata providers that can look up data by file path
/// (e.g., reading sidecar NFO/XML files adjacent to the media file).
/// </summary>
public interface ILocalMetadataProvider
{
    /// <summary>Search for movie metadata from a sidecar file adjacent to the given path.</summary>
    /// <param name="filePath">The path to the media file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of movies found in the sidecar file.</returns>
    Task<IReadOnlyList<Movie>> SearchByFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Search for episode metadata from a sidecar file adjacent to the given path.</summary>
    /// <param name="filePath">The path to the media file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The episode found in the sidecar file, or <see langword="null"/> if not found.</returns>
    Task<Episode?> SearchEpisodeByFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Get enriched movie info from a sidecar file.</summary>
    /// <param name="filePath">The path to the media file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The movie info from the sidecar file, or <see langword="null"/> if not found.</returns>
    Task<MovieInfo?> GetMovieInfoByFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Get series info from tvshow.nfo or equivalent.</summary>
    /// <param name="filePath">The path to the media file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The series info from the sidecar file, or <see langword="null"/> if not found.</returns>
    Task<SeriesInfo?> GetSeriesInfoByFileAsync(string filePath, CancellationToken ct = default);
}
