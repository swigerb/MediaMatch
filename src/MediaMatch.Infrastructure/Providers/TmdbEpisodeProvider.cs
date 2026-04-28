using System.Globalization;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Infrastructure.Caching;
using MediaMatch.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Episode/series provider backed by The Movie Database (TMDb) API v3.
/// </summary>
public sealed class TmdbEpisodeProvider : IEpisodeProvider
{
    private readonly MediaMatchHttpClient _http;
    private readonly MetadataCache _cache;
    private readonly ApiConfiguration _config;
    private readonly ILogger<TmdbEpisodeProvider> _logger;

    /// <inheritdoc />
    public string Name => "TMDb";

    /// <summary>Initializes a new instance of the <see cref="TmdbEpisodeProvider"/> class.</summary>
    /// <param name="http">The HTTP client used for TMDb API requests.</param>
    /// <param name="cache">The metadata cache for storing API responses.</param>
    /// <param name="config">The API configuration containing the TMDb API key.</param>
    /// <param name="logger">The logger instance.</param>
    public TmdbEpisodeProvider(
        MediaMatchHttpClient http,
        MetadataCache cache,
        ApiConfiguration config,
        ILogger<TmdbEpisodeProvider> logger)
    {
        _http = http;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    /// <summary>Gets a value indicating whether a TMDb API key has been configured.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config.TmdbApiKey);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("TMDb API key not configured, skipping series search");
            return Array.Empty<SearchResult>();
        }

        var cacheKey = $"tmdb:tv:search:{query}";
        return await _cache.GetOrCreateAsync<IReadOnlyList<SearchResult>>(cacheKey, async () =>
        {
            var url = $"{_config.TmdbBaseUrl}/search/tv?api_key={_config.TmdbApiKey}&query={Uri.EscapeDataString(query)}&language={_config.Language}";

            _logger.LogDebug("TMDb series search: {Query}", query);

            var response = await _http.GetAsync<TmdbTvSearchResponse>(url, ct).ConfigureAwait(false);
            if (response?.Results is null)
                return Array.Empty<SearchResult>();

            return response.Results
                .Select(r => new SearchResult(
                    Name: r.Name ?? r.OriginalName ?? string.Empty,
                    Id: r.Id))
                .ToList()
                .AsReadOnly();
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Episode>> GetEpisodesAsync(SearchResult series, SortOrder sortOrder = SortOrder.Airdate, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("TMDb API key not configured, skipping episodes lookup");
            return Array.Empty<Episode>();
        }

        var cacheKey = $"tmdb:tv:episodes:{series.Id}:{sortOrder}";
        return await _cache.GetOrCreateAsync<IReadOnlyList<Episode>>(cacheKey, async () =>
        {
            // First get series details to know how many seasons
            var detailUrl = $"{_config.TmdbBaseUrl}/tv/{series.Id}?api_key={_config.TmdbApiKey}&language={_config.Language}";
            var detail = await _http.GetAsync<TmdbTvDetail>(detailUrl, ct).ConfigureAwait(false);

            if (detail?.Seasons is null)
                return Array.Empty<Episode>();

            _logger.LogDebug("TMDb fetching {SeasonCount} seasons for {Series}", detail.Seasons.Count, series.Name);

            var episodes = new List<Episode>();
            int absoluteNumber = 1;

            foreach (var season in detail.Seasons.OrderBy(s => s.SeasonNumber))
            {
                var seasonUrl = $"{_config.TmdbBaseUrl}/tv/{series.Id}/season/{season.SeasonNumber}?api_key={_config.TmdbApiKey}&language={_config.Language}";
                var seasonDetail = await _http.GetAsync<TmdbSeasonDetail>(seasonUrl, ct).ConfigureAwait(false);

                if (seasonDetail?.Episodes is null) continue;

                foreach (var ep in seasonDetail.Episodes.OrderBy(e => e.EpisodeNumber))
                {
                    var isSpecial = season.SeasonNumber == 0;
                    episodes.Add(new Episode(
                        SeriesName: series.Name,
                        Season: season.SeasonNumber,
                        EpisodeNumber: ep.EpisodeNumber,
                        Title: ep.Name ?? string.Empty,
                        AbsoluteNumber: isSpecial ? null : absoluteNumber,
                        Special: isSpecial ? ep.EpisodeNumber : null,
                        AirDate: SimpleDate.TryParse(ep.AirDate),
                        SeriesId: series.Id.ToString(CultureInfo.InvariantCulture),
                        SortOrder: sortOrder));

                    if (!isSpecial) absoluteNumber++;
                }
            }

            return sortOrder switch
            {
                SortOrder.AbsoluteNumber => episodes.OrderBy(e => e.AbsoluteNumber ?? int.MaxValue).ToList().AsReadOnly(),
                SortOrder.DvdOrder => episodes.OrderBy(e => e.Season).ThenBy(e => e.EpisodeNumber).ToList().AsReadOnly(),
                _ => episodes.OrderBy(e => e.AirDate).ToList().AsReadOnly()
            };
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SeriesInfo> GetSeriesInfoAsync(SearchResult series, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("TMDb API key not configured, skipping series info lookup");
            return new SeriesInfo(
                Name: series.Name, Id: series.Id.ToString(CultureInfo.InvariantCulture),
                Overview: null, Network: null, Status: null, Rating: null,
                Runtime: null, Genres: []);
        }

        var cacheKey = $"tmdb:tv:info:{series.Id}";
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var url = $"{_config.TmdbBaseUrl}/tv/{series.Id}?api_key={_config.TmdbApiKey}&language={_config.Language}&append_to_response=external_ids";

            _logger.LogDebug("TMDb series info: {SeriesId}", series.Id);

            var detail = await _http.GetAsync<TmdbTvDetail>(url, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"TMDb returned no data for series {series.Id}");

            var posterUrl = detail.PosterPath is not null
                ? $"{_config.TmdbImageBaseUrl}{detail.PosterPath}"
                : null;

            return new SeriesInfo(
                Name: detail.Name ?? series.Name,
                Id: series.Id.ToString(CultureInfo.InvariantCulture),
                Overview: detail.Overview,
                Network: detail.Networks?.FirstOrDefault()?.Name,
                Status: detail.Status,
                Rating: detail.VoteAverage,
                Runtime: detail.EpisodeRunTime?.FirstOrDefault(),
                Genres: detail.Genres?.Select(g => g.Name ?? string.Empty).ToList() ?? [],
                PosterUrl: posterUrl,
                StartDate: SimpleDate.TryParse(detail.FirstAirDate),
                ImdbId: detail.ExternalIds?.ImdbId,
                TmdbId: detail.Id,
                Language: detail.OriginalLanguage,
                AliasNames: detail.OriginCountry);
        }).ConfigureAwait(false);
    }

    #region TMDb JSON DTOs

    private sealed class TmdbTvSearchResponse
    {
        public List<TmdbTvSearchResult>? Results { get; set; }
    }

    private sealed class TmdbTvSearchResult
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? OriginalName { get; set; }
    }

    private sealed class TmdbTvDetail
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Overview { get; set; }
        public string? Status { get; set; }
        public string? PosterPath { get; set; }
        public double? VoteAverage { get; set; }
        public string? FirstAirDate { get; set; }
        public string? OriginalLanguage { get; set; }
        public List<int>? EpisodeRunTime { get; set; }
        public List<string>? OriginCountry { get; set; }
        public List<TmdbGenre>? Genres { get; set; }
        public List<TmdbNetwork>? Networks { get; set; }
        public List<TmdbSeason>? Seasons { get; set; }
        public TmdbExternalIds? ExternalIds { get; set; }
    }

    private sealed class TmdbGenre { public string? Name { get; set; } }
    private sealed class TmdbNetwork { public string? Name { get; set; } }

    private sealed class TmdbSeason
    {
        public int SeasonNumber { get; set; }
    }

    private sealed class TmdbSeasonDetail
    {
        public List<TmdbEpisode>? Episodes { get; set; }
    }

    private sealed class TmdbEpisode
    {
        public int EpisodeNumber { get; set; }
        public string? Name { get; set; }
        public string? AirDate { get; set; }
    }

    private sealed class TmdbExternalIds
    {
        public string? ImdbId { get; set; }
    }

    #endregion
}
