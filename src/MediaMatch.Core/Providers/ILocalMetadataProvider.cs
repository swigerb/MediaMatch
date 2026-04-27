using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

/// <summary>
/// Marker interface for metadata providers that can look up data by file path
/// (e.g., reading sidecar NFO/XML files adjacent to the media file).
/// </summary>
public interface ILocalMetadataProvider
{
    /// <summary>Search for movie metadata from a sidecar file adjacent to the given path.</summary>
    Task<IReadOnlyList<Movie>> SearchByFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Search for episode metadata from a sidecar file adjacent to the given path.</summary>
    Task<Episode?> SearchEpisodeByFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Get enriched movie info from a sidecar file.</summary>
    Task<MovieInfo?> GetMovieInfoByFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Get series info from tvshow.nfo or equivalent.</summary>
    Task<SeriesInfo?> GetSeriesInfoByFileAsync(string filePath, CancellationToken ct = default);
}
