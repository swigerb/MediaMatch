using MediaMatch.Core.Models;

namespace MediaMatch.Core.Providers;

public interface IEpisodeProvider
{
    string Name { get; }

    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default);

    Task<IReadOnlyList<Episode>> GetEpisodesAsync(SearchResult series, SortOrder sortOrder = SortOrder.Airdate, CancellationToken ct = default);

    Task<SeriesInfo> GetSeriesInfoAsync(SearchResult series, CancellationToken ct = default);
}
