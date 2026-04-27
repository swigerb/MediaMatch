using System.Globalization;
using System.Xml.Linq;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Fallback provider that maps AniDB anime IDs to TVDb series IDs
/// using the community-maintained anime-lists mapping XML.
/// When AniDB direct lookup fails, falls back to TVDb via the mapping.
/// </summary>
public sealed class AniDbTvdbMappingProvider
{
    private readonly HttpClient _http;
    private readonly MetadataCache _cache;
    private readonly AniDbConfiguration _config;
    private readonly IReadOnlyList<IEpisodeProvider> _episodeProviders;
    private readonly ILogger<AniDbTvdbMappingProvider> _logger;

    private readonly SemaphoreSlim _mappingLock = new(1, 1);
    private Dictionary<int, int>? _anidbToTvdbMap;
    private DateTimeOffset _mappingLoadedAt = DateTimeOffset.MinValue;

    public AniDbTvdbMappingProvider(
        HttpClient http,
        MetadataCache cache,
        AniDbConfiguration config,
        IEnumerable<IEpisodeProvider> episodeProviders,
        ILogger<AniDbTvdbMappingProvider> logger)
    {
        _http = http;
        _cache = cache;
        _config = config;
        _episodeProviders = episodeProviders.ToList();
        _logger = logger;

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MediaMatch/1.0");
    }

    /// <summary>
    /// Attempts to map an AniDB anime ID to a TVDb series ID.
    /// Returns null if no mapping is found.
    /// </summary>
    public async Task<int?> MapAniDbToTvdbAsync(int anidbId, CancellationToken ct = default)
    {
        var mapping = await GetMappingAsync(ct);
        return mapping.TryGetValue(anidbId, out var tvdbId) ? tvdbId : null;
    }

    /// <summary>
    /// Attempts to map a TVDb series ID to an AniDB anime ID.
    /// Returns null if no mapping is found.
    /// </summary>
    public async Task<int?> MapTvdbToAniDbAsync(int tvdbId, CancellationToken ct = default)
    {
        var mapping = await GetMappingAsync(ct);
        var entry = mapping.FirstOrDefault(kv => kv.Value == tvdbId);
        return entry.Key != 0 ? entry.Key : null;
    }

    /// <summary>
    /// Falls back to TVDb when AniDB direct lookup fails.
    /// Maps the AniDB ID to a TVDb ID and queries the TVDb provider.
    /// </summary>
    public async Task<IReadOnlyList<Episode>> GetEpisodesViaTvdbFallbackAsync(
        int anidbId, string seriesName, CancellationToken ct = default)
    {
        var tvdbId = await MapAniDbToTvdbAsync(anidbId, ct);
        if (tvdbId is null)
        {
            _logger.LogDebug("No TVDb mapping found for AniDB anime {AniDbId}", anidbId);
            return Array.Empty<Episode>();
        }

        var tvdbProvider = _episodeProviders.FirstOrDefault(p => p.Name == "TVDb");
        if (tvdbProvider is null)
        {
            _logger.LogWarning("TVDb provider not available for fallback");
            return Array.Empty<Episode>();
        }

        _logger.LogInformation("Falling back to TVDb (ID={TvdbId}) for AniDB anime {AniDbId}", tvdbId, anidbId);

        var searchResult = new SearchResult(seriesName, tvdbId.Value);
        return await tvdbProvider.GetEpisodesAsync(searchResult, SortOrder.Airdate, ct);
    }

    /// <summary>
    /// Falls back to TVDb for series info when AniDB direct lookup fails.
    /// </summary>
    public async Task<SeriesInfo?> GetSeriesInfoViaTvdbFallbackAsync(
        int anidbId, string seriesName, CancellationToken ct = default)
    {
        var tvdbId = await MapAniDbToTvdbAsync(anidbId, ct);
        if (tvdbId is null)
            return null;

        var tvdbProvider = _episodeProviders.FirstOrDefault(p => p.Name == "TVDb");
        if (tvdbProvider is null)
            return null;

        var searchResult = new SearchResult(seriesName, tvdbId.Value);
        return await tvdbProvider.GetSeriesInfoAsync(searchResult, ct);
    }

    // ── Mapping file management ──────────────────────────────────

    private async Task<Dictionary<int, int>> GetMappingAsync(CancellationToken ct)
    {
        var cacheExpiry = TimeSpan.FromHours(_config.MappingCacheHours);
        var now = DateTimeOffset.UtcNow;

        if (_anidbToTvdbMap is not null && (now - _mappingLoadedAt) < cacheExpiry)
            return _anidbToTvdbMap;

        await _mappingLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_anidbToTvdbMap is not null && (now - _mappingLoadedAt) < cacheExpiry)
                return _anidbToTvdbMap;

            _logger.LogInformation("Downloading AniDB-TVDb mapping file from {Url}", _config.TvdbMappingUrl);

            var response = await _http.GetAsync(_config.TvdbMappingUrl, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var doc = XDocument.Parse(content);

            var map = new Dictionary<int, int>();

            foreach (var anime in doc.Descendants("anime"))
            {
                var anidbIdStr = anime.Attribute("anidbid")?.Value;
                var tvdbIdStr = anime.Attribute("tvdbid")?.Value;

                if (anidbIdStr is not null && tvdbIdStr is not null
                    && int.TryParse(anidbIdStr, CultureInfo.InvariantCulture, out var anidbId)
                    && int.TryParse(tvdbIdStr, CultureInfo.InvariantCulture, out var tvdbId)
                    && tvdbId > 0)
                {
                    map[anidbId] = tvdbId;
                }
            }

            _anidbToTvdbMap = map;
            _mappingLoadedAt = DateTimeOffset.UtcNow;

            _logger.LogInformation("Loaded {Count} AniDB-TVDb mappings", map.Count);
            return map;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download AniDB-TVDb mapping file");
            return _anidbToTvdbMap ?? new Dictionary<int, int>();
        }
        finally
        {
            _mappingLock.Release();
        }
    }
}
