using MediaMatch.Core.Configuration;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Infrastructure.Caching;
using MediaMatch.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace MediaMatch.Infrastructure.Providers;

/// <summary>
/// Artwork provider backed by The Movie Database (TMDb) API v3.
/// Retrieves posters, backdrops, and season art for both TV series and movies.
/// </summary>
public sealed class TmdbArtworkProvider : IArtworkProvider
{
    private readonly MediaMatchHttpClient _http;
    private readonly MetadataCache _cache;
    private readonly ApiConfiguration _config;
    private readonly ILogger<TmdbArtworkProvider> _logger;

    /// <inheritdoc />
    public string Name => "TMDb";

    /// <summary>Initializes a new instance of the <see cref="TmdbArtworkProvider"/> class.</summary>
    /// <param name="http">The HTTP client used for TMDb API requests.</param>
    /// <param name="cache">The metadata cache for storing API responses.</param>
    /// <param name="config">The API configuration containing the TMDb API key.</param>
    /// <param name="logger">The logger instance.</param>
    public TmdbArtworkProvider(
        MediaMatchHttpClient http,
        MetadataCache cache,
        ApiConfiguration config,
        ILogger<TmdbArtworkProvider> logger)
    {
        _http = http;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    /// <summary>Gets a value indicating whether a TMDb API key has been configured.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config.TmdbApiKey);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Artwork>> GetArtworkAsync(int tvdbId, ArtworkType? type = null, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("TMDb API key not configured, skipping TV artwork lookup");
            return Array.Empty<Artwork>();
        }

        // TMDb uses its own IDs. We use tvdbId as TMDb series ID here
        // (callers should pass TMDb IDs; the parameter name comes from the interface).
        var cacheKey = $"tmdb:artwork:tv:{tvdbId}:{type}";
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var url = $"{_config.TmdbBaseUrl}/tv/{tvdbId}/images?api_key={_config.TmdbApiKey}";

            _logger.LogDebug("TMDb TV artwork: {SeriesId}", tvdbId);

            var response = await _http.GetAsync<TmdbImagesResponse>(url, ct).ConfigureAwait(false);
            return MapArtwork(response, type);
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Artwork>> GetMovieArtworkAsync(int tmdbId, ArtworkType? type = null, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("TMDb API key not configured, skipping movie artwork lookup");
            return Array.Empty<Artwork>();
        }

        var cacheKey = $"tmdb:artwork:movie:{tmdbId}:{type}";
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var url = $"{_config.TmdbBaseUrl}/movie/{tmdbId}/images?api_key={_config.TmdbApiKey}";

            _logger.LogDebug("TMDb movie artwork: {MovieId}", tmdbId);

            var response = await _http.GetAsync<TmdbImagesResponse>(url, ct).ConfigureAwait(false);
            return MapArtwork(response, type);
        }).ConfigureAwait(false);
    }

    private IReadOnlyList<Artwork> MapArtwork(TmdbImagesResponse? response, ArtworkType? typeFilter)
    {
        if (response is null)
            return Array.Empty<Artwork>();

        var artwork = new List<Artwork>();

        if (response.Posters is not null)
        {
            artwork.AddRange(response.Posters.Select(img => new Artwork(
                Url: $"{_config.TmdbImageBaseUrl}{img.FilePath}",
                Type: ArtworkType.Poster,
                Language: img.Iso639_1,
                Rating: img.VoteAverage,
                Width: img.Width,
                Height: img.Height)));
        }

        if (response.Backdrops is not null)
        {
            artwork.AddRange(response.Backdrops.Select(img => new Artwork(
                Url: $"{_config.TmdbImageBaseUrl}{img.FilePath}",
                Type: ArtworkType.Fanart,
                Language: img.Iso639_1,
                Rating: img.VoteAverage,
                Width: img.Width,
                Height: img.Height)));
        }

        if (response.Logos is not null)
        {
            artwork.AddRange(response.Logos.Select(img => new Artwork(
                Url: $"{_config.TmdbImageBaseUrl}{img.FilePath}",
                Type: ArtworkType.Clearlogo,
                Language: img.Iso639_1,
                Rating: img.VoteAverage,
                Width: img.Width,
                Height: img.Height)));
        }

        if (typeFilter.HasValue)
            return artwork.Where(a => a.Type == typeFilter.Value).ToList().AsReadOnly();

        return artwork.AsReadOnly();
    }

    #region TMDb JSON DTOs

    private sealed class TmdbImagesResponse
    {
        public List<TmdbImage>? Posters { get; set; }
        public List<TmdbImage>? Backdrops { get; set; }
        public List<TmdbImage>? Logos { get; set; }
    }

    private sealed class TmdbImage
    {
        public string? FilePath { get; set; }
        public string? Iso639_1 { get; set; }
        public double? VoteAverage { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }

    #endregion
}
