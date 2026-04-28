using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

/// <summary>
/// Provides episode metadata search, episode listing, and series info retrieval.
/// </summary>
public interface IEpisodeProvider
{
    /// <summary>Gets the display name of this provider.</summary>
    string Name { get; }

    /// <summary>
    /// Searches for series matching the specified query.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of matching search results.</returns>
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the episode list for the specified series.
    /// </summary>
    /// <param name="series">The series to retrieve episodes for.</param>
    /// <param name="sortOrder">The sort order for the returned episodes.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of episodes.</returns>
    Task<IReadOnlyList<Episode>> GetEpisodesAsync(SearchResult series, SortOrder sortOrder = SortOrder.Airdate, CancellationToken ct = default);

    /// <summary>
    /// Retrieves detailed series information for the specified search result.
    /// </summary>
    /// <param name="series">The series to retrieve information for.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The detailed series information.</returns>
    Task<SeriesInfo> GetSeriesInfoAsync(SearchResult series, CancellationToken ct = default);
}
