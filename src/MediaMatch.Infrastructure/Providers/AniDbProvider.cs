using System.Globalization;
using System.Xml.Linq;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Episode/series provider backed by the AniDB HTTP API.
/// Enforces AniDB's strict rate limit of ≤1 request per 2 seconds.
/// Uses XML parsing since AniDB returns XML responses.
/// </summary>
public sealed class AniDbProvider : IAniDbProvider
{
    private readonly HttpClient _http;
    private readonly MetadataCache _cache;
    private readonly AniDbConfiguration _config;
    private readonly ILogger<AniDbProvider> _logger;

    private readonly SemaphoreSlim _rateLimitGate = new(1, 1);
    private DateTimeOffset _lastRequestTime = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public string Name => "AniDB";

    /// <summary>Initializes a new instance of the <see cref="AniDbProvider"/> class.</summary>
    /// <param name="http">The HTTP client used for AniDB API requests.</param>
    /// <param name="cache">The metadata cache for storing API responses.</param>
    /// <param name="config">The AniDB-specific configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public AniDbProvider(
        HttpClient http,
        MetadataCache cache,
        AniDbConfiguration config,
        ILogger<AniDbProvider> logger)
    {
        _http = http;
        _cache = cache;
        _config = config;
        _logger = logger;

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MediaMatch/1.0");
        _http.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct = default)
        => await SearchAnimeAsync(query, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAnimeAsync(string title, CancellationToken ct = default)
    {
        var cacheKey = $"anidb:search:{title}";
        return await _cache.GetOrCreateAsync<IReadOnlyList<SearchResult>>(cacheKey, async () =>
        {
            _logger.LogDebug("AniDB anime search: {Title}", title);

            // AniDB HTTP API does not have a direct search endpoint.
            // We use the anime lookup approach with the title as a query parameter.
            var url = BuildApiUrl("anime", ("s", title));

            var xml = await GetXmlAsync(url, ct).ConfigureAwait(false);
            if (xml is null)
                return Array.Empty<SearchResult>();

            var results = new List<SearchResult>();
            var animeElements = xml.Descendants("anime");

            foreach (var anime in animeElements)
            {
                var aidAttr = anime.Attribute("id");
                if (aidAttr is null || !int.TryParse(aidAttr.Value, out var aid))
                    continue;

                var mainTitle = anime.Descendants("title")
                    .FirstOrDefault(t => t.Attribute("type")?.Value == "main")
                    ?.Value ?? anime.Element("title")?.Value ?? string.Empty;

                var aliases = anime.Descendants("title")
                    .Where(t => t.Attribute("type")?.Value is "official" or "synonym")
                    .Select(t => t.Value)
                    .ToList();

                results.Add(new SearchResult(
                    Name: mainTitle,
                    Id: aid,
                    AliasNames: aliases.Count > 0 ? aliases : null));
            }

            return results.AsReadOnly();
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Episode>> GetEpisodesAsync(
        SearchResult series, SortOrder sortOrder = SortOrder.Airdate, CancellationToken ct = default)
        => await GetAnimeEpisodesAsync(series.Id, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Episode>> GetAnimeEpisodesAsync(int animeId, CancellationToken ct = default)
    {
        var cacheKey = $"anidb:episodes:{animeId}";
        return await _cache.GetOrCreateAsync<IReadOnlyList<Episode>>(cacheKey, async () =>
        {
            _logger.LogDebug("AniDB fetching episodes for anime {AnimeId}", animeId);

            var url = BuildApiUrl("anime", ("aid", animeId.ToString(CultureInfo.InvariantCulture)));
            var xml = await GetXmlAsync(url, ct).ConfigureAwait(false);
            if (xml is null)
                return Array.Empty<Episode>();

            var seriesName = xml.Descendants("title")
                .FirstOrDefault(t => t.Attribute("type")?.Value == "main")
                ?.Value ?? $"Anime {animeId}";

            var episodes = new List<Episode>();
            var epElements = xml.Descendants("episode");

            foreach (var ep in epElements)
            {
                var epnoElement = ep.Element("epno");
                if (epnoElement is null) continue;

                var epnoType = epnoElement.Attribute("type")?.Value ?? "1";
                var epnoText = epnoElement.Value;

                int season;
                int episodeNumber;
                int? special = null;

                if (epnoType == "1" && int.TryParse(epnoText, out var normalEp))
                {
                    // Regular episode
                    season = 1;
                    episodeNumber = normalEp;
                }
                else if (epnoType == "2")
                {
                    // Special
                    season = 0;
                    int.TryParse(epnoText.TrimStart('S'), out var specNum);
                    episodeNumber = specNum;
                    special = specNum;
                }
                else
                {
                    continue; // Skip credits, trailers, etc.
                }

                var title = ep.Elements("title")
                    .FirstOrDefault(t => t.Attribute("{http://www.w3.org/XML/1998/namespace}lang")?.Value == "en")
                    ?.Value ?? ep.Element("title")?.Value ?? string.Empty;

                var airDateStr = ep.Element("airdate")?.Value;

                episodes.Add(new Episode(
                    SeriesName: seriesName,
                    Season: season,
                    EpisodeNumber: episodeNumber,
                    Title: title,
                    AbsoluteNumber: epnoType == "1" ? episodeNumber : null,
                    Special: special,
                    AirDate: SimpleDate.TryParse(airDateStr),
                    SeriesId: animeId.ToString(CultureInfo.InvariantCulture),
                    SortOrder: SortOrder.AbsoluteNumber));
            }

            return episodes.OrderBy(e => e.Season).ThenBy(e => e.EpisodeNumber).ToList().AsReadOnly();
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SeriesInfo> GetSeriesInfoAsync(SearchResult series, CancellationToken ct = default)
        => await GetAnimeInfoAsync(series.Id, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<SeriesInfo> GetAnimeInfoAsync(int animeId, CancellationToken ct = default)
    {
        var cacheKey = $"anidb:info:{animeId}";
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            _logger.LogDebug("AniDB series info: {AnimeId}", animeId);

            var url = BuildApiUrl("anime", ("aid", animeId.ToString(CultureInfo.InvariantCulture)));
            var xml = await GetXmlAsync(url, ct).ConfigureAwait(false);
            if (xml is null)
                throw new InvalidOperationException($"AniDB returned no data for anime {animeId}");

            var mainTitle = xml.Descendants("title")
                .FirstOrDefault(t => t.Attribute("type")?.Value == "main")
                ?.Value ?? $"Anime {animeId}";

            var description = xml.Element("description")?.Value;

            var startDateStr = xml.Element("startdate")?.Value;
            var endDateStr = xml.Element("enddate")?.Value;

            var rating = xml.Element("ratings")?.Element("permanent")?.Value;
            double? ratingValue = rating is not null && double.TryParse(rating, CultureInfo.InvariantCulture, out var r) ? r : null;

            var genres = xml.Descendants("tag")
                .Select(t => t.Element("name")?.Value)
                .Where(n => n is not null)
                .Cast<string>()
                .Take(10)
                .ToList();

            var aliases = xml.Descendants("title")
                .Where(t => t.Attribute("type")?.Value is "official" or "synonym")
                .Select(t => t.Value)
                .ToList();

            var type = xml.Element("type")?.Value;

            return new SeriesInfo(
                Name: mainTitle,
                Id: animeId.ToString(CultureInfo.InvariantCulture),
                Overview: description,
                Network: null,
                Status: type,
                Rating: ratingValue,
                Runtime: null,
                Genres: genres,
                StartDate: SimpleDate.TryParse(startDateStr),
                AliasNames: aliases.Count > 0 ? aliases : null);
        }).ConfigureAwait(false);
    }

    // ── Rate-limited XML fetching ────────────────────────────────

    private async Task<XElement?> GetXmlAsync(string url, CancellationToken ct)
    {
        await EnforceRateLimitAsync(ct).ConfigureAwait(false);

        int attempt = 0;
        while (true)
        {
            try
            {
                using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);

                if ((int)response.StatusCode == 429)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(4);
                    _logger.LogWarning("AniDB rate limited, backing off {Seconds}s", retryAfter.TotalSeconds);
                    await Task.Delay(retryAfter, ct).ConfigureAwait(false);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(content))
                    return null;

                var doc = XDocument.Parse(content);

                // AniDB returns <error> element on failures
                var errorElement = doc.Root?.Element("error");
                if (errorElement is not null)
                {
                    _logger.LogWarning("AniDB API error: {Error}", errorElement.Value);
                    return null;
                }

                return doc.Root;
            }
            catch (HttpRequestException ex) when (attempt < _config.MaxRetries && IsTransient(ex))
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Transient failure on AniDB, retry {Attempt}/{Max} in {Delay}s",
                    attempt, _config.MaxRetries, delay.TotalSeconds);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task EnforceRateLimitAsync(CancellationToken ct)
    {
        await _rateLimitGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var elapsed = now - _lastRequestTime;
            var minInterval = TimeSpan.FromMilliseconds(_config.RateLimitIntervalMs);

            if (elapsed < minInterval)
            {
                var waitTime = minInterval - elapsed;
                _logger.LogDebug("AniDB rate limit: waiting {Ms}ms", waitTime.TotalMilliseconds);
                await Task.Delay(waitTime, ct).ConfigureAwait(false);
            }

            _lastRequestTime = DateTimeOffset.UtcNow;
        }
        finally
        {
            _rateLimitGate.Release();
        }
    }

    private string BuildApiUrl(string request, params (string key, string value)[] parameters)
    {
        var url = $"{_config.BaseUrl}?request={request}&client={Uri.EscapeDataString(_config.ClientName)}&clientver={_config.ClientVersion}&protover={_config.ProtocolVersion}";

        foreach (var (key, value) in parameters)
        {
            url += $"&{key}={Uri.EscapeDataString(value)}";
        }

        return url;
    }

    private static bool IsTransient(HttpRequestException ex)
    {
        return ex.StatusCode is System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout
            or System.Net.HttpStatusCode.RequestTimeout
            or null;
    }
}
