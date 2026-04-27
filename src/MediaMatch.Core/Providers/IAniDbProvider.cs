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
    Task<IReadOnlyList<SearchResult>> SearchAnimeAsync(string title, CancellationToken ct = default);

    /// <summary>
    /// Retrieves full episode list for an anime by its AniDB ID.
    /// </summary>
    Task<IReadOnlyList<Episode>> GetAnimeEpisodesAsync(int animeId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves series metadata for an anime by its AniDB ID.
    /// </summary>
    Task<SeriesInfo> GetAnimeInfoAsync(int animeId, CancellationToken ct = default);
}
