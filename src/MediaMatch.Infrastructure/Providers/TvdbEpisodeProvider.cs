using System.Globalization;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Infrastructure.Caching;
using MediaMatch.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Episode/series provider backed by TheTVDB API v4.
/// Requires a valid API key for bearer-token authentication.
/// </summary>
public sealed class TvdbEpisodeProvider : IEpisodeProvider
{
    private readonly MediaMatchHttpClient _http;
    private readonly MetadataCache _cache;
    private readonly ApiConfiguration _config;
    private readonly ILogger<TvdbEpisodeProvider> _logger;

    private string? _bearerToken;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    /// <inheritdoc />
    public string Name => "TVDb";

    /// <summary>Initializes a new instance of the <see cref="TvdbEpisodeProvider"/> class.</summary>
    /// <param name="http">The HTTP client used for TVDb API requests.</param>
    /// <param name="cache">The metadata cache for storing API responses.</param>
    /// <param name="config">The API configuration containing the TVDb API key.</param>
    /// <param name="logger">The logger instance.</param>
    public TvdbEpisodeProvider(
        MediaMatchHttpClient http,
        MetadataCache cache,
        ApiConfiguration config,
        ILogger<TvdbEpisodeProvider> logger)
    {
        _http = http;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    /// <summary>Gets a value indicating whether a TVDb API key has been configured.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config.TvdbApiKey);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("TVDb API key not configured, skipping series search");
            return Array.Empty<SearchResult>();
        }

        var cacheKey = $"tvdb:search:{query}";
        return await _cache.GetOrCreateAsync<IReadOnlyList<SearchResult>>(cacheKey, async () =>
        {
            await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);

            var url = $"{_config.TvdbBaseUrl}/search?query={Uri.EscapeDataString(query)}&type=series";

            _logger.LogDebug("TVDb series search: {Query}", query);

            var response = await _http.GetAsync<TvdbResponse<List<TvdbSearchResult>>>(url, ct).ConfigureAwait(false);
            if (response?.Data is null)
                return Array.Empty<SearchResult>();

            return response.Data
                .Where(r => int.TryParse(r.TvdbId, out _))
                .Select(r => new SearchResult(
                    Name: r.Name ?? string.Empty,
                    Id: int.Parse(r.TvdbId!, CultureInfo.InvariantCulture),
                    AliasNames: r.Aliases))
                .ToList()
                .AsReadOnly();
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Episode>> GetEpisodesAsync(SearchResult series, SortOrder sortOrder = SortOrder.Airdate, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("TVDb API key not configured, skipping episodes lookup");
            return Array.Empty<Episode>();
        }

        var cacheKey = $"tvdb:episodes:{series.Id}:{sortOrder}";
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);

            var seasonType = sortOrder switch
            {
                SortOrder.DvdOrder => "dvd",
                SortOrder.AbsoluteNumber => "absolute",
                _ => "default"
            };

            _logger.LogDebug("TVDb fetching episodes for {Series} (order={Order})", series.Name, seasonType);

            var episodes = new List<Episode>();
            int page = 0;

            while (true)
            {
                var url = $"{_config.TvdbBaseUrl}/series/{series.Id}/episodes/{seasonType}?page={page}";
                var response = await _http.GetAsync<TvdbResponse<TvdbEpisodesData>>(url, ct).ConfigureAwait(false);

                if (response?.Data?.Episodes is null || response.Data.Episodes.Count == 0)
                    break;

                foreach (var ep in response.Data.Episodes)
                {
                    episodes.Add(new Episode(
                        SeriesName: series.Name,
                        Season: ep.SeasonNumber,
                        EpisodeNumber: ep.Number,
                        Title: ep.Name ?? string.Empty,
                        AbsoluteNumber: ep.AbsoluteNumber,
                        Special: ep.SeasonNumber == 0 ? ep.Number : null,
                        AirDate: SimpleDate.TryParse(ep.Aired),
                        SeriesId: series.Id.ToString(CultureInfo.InvariantCulture),
                        SortOrder: sortOrder));
                }

                // TVDb v4 paginates; check if there's more
                if (response.Data.Episodes.Count < 500)
                    break;

                page++;
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
            _logger.LogDebug("TVDb API key not configured, skipping series info lookup");
            return new SeriesInfo(
                Name: series.Name, Id: series.Id.ToString(CultureInfo.InvariantCulture),
                Overview: null, Network: null, Status: null, Rating: null,
                Runtime: null, Genres: []);
        }

        var cacheKey = $"tvdb:info:{series.Id}";
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);

            var url = $"{_config.TvdbBaseUrl}/series/{series.Id}/extended";

            _logger.LogDebug("TVDb series info: {SeriesId}", series.Id);

            var response = await _http.GetAsync<TvdbResponse<TvdbSeriesDetail>>(url, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"TVDb returned no data for series {series.Id}");

            var detail = response.Data
                ?? throw new InvalidOperationException($"TVDb returned empty data for series {series.Id}");

            var posterUrl = detail.Image;

            return new SeriesInfo(
                Name: detail.Name ?? series.Name,
                Id: series.Id.ToString(CultureInfo.InvariantCulture),
                Overview: detail.Overview,
                Network: detail.OriginalNetwork?.Name,
                Status: detail.Status?.Name,
                Rating: detail.Score,
                Runtime: detail.AverageRuntime,
                Genres: detail.Genres?.Select(g => g.Name ?? string.Empty).ToList() ?? [],
                PosterUrl: posterUrl,
                StartDate: SimpleDate.TryParse(detail.FirstAired),
                Language: detail.OriginalLanguage,
                AliasNames: detail.Aliases?.Select(a => a.Name ?? string.Empty).ToList());
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Authenticates with TVDb v4 API using the configured API key.
    /// Token is cached for the lifetime of this provider instance.
    /// </summary>
    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (_bearerToken is not null) return;

        await _authLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_bearerToken is not null) return;

            var url = $"{_config.TvdbBaseUrl}/login";
            var response = await _http.PostAsync<TvdbLoginRequest, TvdbResponse<TvdbLoginData>>(
                url,
                new TvdbLoginRequest { Apikey = _config.TvdbApiKey },
                ct).ConfigureAwait(false);

            _bearerToken = response?.Data?.Token
                ?? throw new InvalidOperationException("Failed to authenticate with TVDb API");

            _logger.LogInformation("TVDb authentication successful");
        }
        finally
        {
            _authLock.Release();
        }
    }

    #region TVDb JSON DTOs

    private sealed class TvdbResponse<T>
    {
        public string? Status { get; set; }
        public T? Data { get; set; }
    }

    private sealed class TvdbLoginRequest
    {
        public string? Apikey { get; set; }
    }

    private sealed class TvdbLoginData
    {
        public string? Token { get; set; }
    }

    private sealed class TvdbSearchResult
    {
        public string? TvdbId { get; set; }
        public string? Name { get; set; }
        public List<string>? Aliases { get; set; }
    }

    private sealed class TvdbEpisodesData
    {
        public List<TvdbEpisode>? Episodes { get; set; }
    }

    private sealed class TvdbEpisode
    {
        public int SeasonNumber { get; set; }
        public int Number { get; set; }
        public string? Name { get; set; }
        public string? Aired { get; set; }
        public int? AbsoluteNumber { get; set; }
    }

    private sealed class TvdbSeriesDetail
    {
        public string? Name { get; set; }
        public string? Overview { get; set; }
        public string? Image { get; set; }
        public string? FirstAired { get; set; }
        public string? OriginalLanguage { get; set; }
        public double? Score { get; set; }
        public int? AverageRuntime { get; set; }
        public TvdbNetwork? OriginalNetwork { get; set; }
        public TvdbStatus? Status { get; set; }
        public List<TvdbGenre>? Genres { get; set; }
        public List<TvdbAlias>? Aliases { get; set; }
    }

    private sealed class TvdbNetwork { public string? Name { get; set; } }
    private sealed class TvdbStatus { public string? Name { get; set; } }
    private sealed class TvdbGenre { public string? Name { get; set; } }
    private sealed class TvdbAlias { public string? Name { get; set; } }

    #endregion
}
