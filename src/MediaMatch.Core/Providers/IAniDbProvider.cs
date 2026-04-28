using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

/// <summary>
/// AniDB-specific episode provider with anime title lookup,
/// episode data, and series info retrieval.
/// </summary>
public interface IAniDbProvider : IEpisodeProvider
{
    /// <summary>
    /// Searches for anime by title using AniDB's HTTP API.
    /// </summary>
    /// <param name="title">The anime title to search for.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of matching search results.</returns>
    Task<IReadOnlyList<SearchResult>> SearchAnimeAsync(string title, CancellationToken ct = default);

    /// <summary>
    /// Retrieves full episode list for an anime by its AniDB ID.
    /// </summary>
    /// <param name="animeId">The AniDB anime identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of episodes.</returns>
    Task<IReadOnlyList<Episode>> GetAnimeEpisodesAsync(int animeId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves series metadata for an anime by its AniDB ID.
    /// </summary>
    /// <param name="animeId">The AniDB anime identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The series information.</returns>
    Task<SeriesInfo> GetAnimeInfoAsync(int animeId, CancellationToken ct = default);
}
