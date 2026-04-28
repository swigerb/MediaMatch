using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

/// <summary>
/// Provides artwork retrieval for series and movies.
/// </summary>
public interface IArtworkProvider
{
    /// <summary>Gets the display name of this provider.</summary>
    string Name { get; }

    /// <summary>
    /// Retrieves artwork for the specified series.
    /// </summary>
    /// <param name="tvdbId">The TVDb identifier for the series.</param>
    /// <param name="type">An optional artwork type filter.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of artwork items.</returns>
    Task<IReadOnlyList<Artwork>> GetArtworkAsync(int tvdbId, ArtworkType? type = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves artwork for the specified movie.
    /// </summary>
    /// <param name="tmdbId">The TMDb identifier for the movie.</param>
    /// <param name="type">An optional artwork type filter.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of artwork items.</returns>
    Task<IReadOnlyList<Artwork>> GetMovieArtworkAsync(int tmdbId, ArtworkType? type = null, CancellationToken ct = default);
}
