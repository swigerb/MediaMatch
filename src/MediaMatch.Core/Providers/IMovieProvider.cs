using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

/// <summary>
/// Provides movie metadata search and detail retrieval.
/// </summary>
public interface IMovieProvider
{
    /// <summary>Gets the display name of this provider.</summary>
    string Name { get; }

    /// <summary>
    /// Searches for movies matching the specified query.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="year">An optional release year to narrow results.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of matching movies.</returns>
    Task<IReadOnlyList<Movie>> SearchAsync(string query, int? year = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves detailed information for the specified movie.
    /// </summary>
    /// <param name="movie">The movie to retrieve details for.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The detailed movie information.</returns>
    Task<MovieInfo> GetMovieInfoAsync(Movie movie, CancellationToken ct = default);
}
