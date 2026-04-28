using System.Diagnostics;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Enums;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Services;

/// <summary>
/// Ordered metadata provider pipeline: NFO → XML → TMDb → TVDb → AniDB.
/// Short-circuits on first high-confidence match (≥0.90 for local, ≥0.85 for online).
/// </summary>
public sealed class MetadataProviderChain
{
    private static readonly ActivitySource ActivitySrc = new("MediaMatch", "0.1.0");

    private const float LocalConfidenceThreshold = 0.90f;
    private const float OnlineConfidenceThreshold = 0.85f;

    private static readonly HashSet<string> LocalProviderNames =
        new(StringComparer.OrdinalIgnoreCase) { "NFO", "XML" };

    private readonly IReadOnlyList<IMovieProvider> _movieProviders;
    private readonly IReadOnlyList<IEpisodeProvider> _episodeProviders;
    private readonly bool _preferLocalMetadata;
    private readonly ILogger<MetadataProviderChain> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataProviderChain"/> class.
    /// </summary>
    /// <param name="movieProviders">The available movie metadata providers.</param>
    /// <param name="episodeProviders">The available episode metadata providers.</param>
    /// <param name="appSettings">Optional application settings controlling provider ordering.</param>
    /// <param name="logger">Optional logger instance.</param>
    public MetadataProviderChain(
        IEnumerable<IMovieProvider> movieProviders,
        IEnumerable<IEpisodeProvider> episodeProviders,
        AppSettings? appSettings = null,
        ILogger<MetadataProviderChain>? logger = null)
    {
        _preferLocalMetadata = appSettings?.PreferLocalMetadata ?? true;
        _logger = logger ?? NullLogger<MetadataProviderChain>.Instance;

        var allMovie = movieProviders.ToList();
        var allEpisode = episodeProviders.ToList();

        if (_preferLocalMetadata)
        {
            _movieProviders = OrderProviders(allMovie);
            _episodeProviders = OrderProviders(allEpisode);
        }
        else
        {
            _movieProviders = allMovie;
            _episodeProviders = allEpisode;
        }
    }

    /// <summary>All movie providers in priority order.</summary>
    public IReadOnlyList<IMovieProvider> MovieProviders => _movieProviders;

    /// <summary>All episode providers in priority order.</summary>
    public IReadOnlyList<IEpisodeProvider> EpisodeProviders => _episodeProviders;

    /// <summary>
    /// Match a file against movie providers in priority order.
    /// </summary>
    public async Task<MatchResult> MatchMovieAsync(string filePath, float baseConfidence, CancellationToken ct = default)
    {
        using var activity = ActivitySrc.StartActivity("mediamatch.chain.movie");
        MatchResult? best = null;

        foreach (var provider in _movieProviders)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                IReadOnlyList<Movie> movies;

                if (provider is ILocalMetadataProvider local)
                    movies = await local.SearchByFileAsync(filePath, ct).ConfigureAwait(false);
                else
                    movies = await provider.SearchAsync(
                        Path.GetFileNameWithoutExtension(filePath), null, ct).ConfigureAwait(false);

                if (movies.Count == 0) continue;

                var movie = movies[0];
                var movieInfo = provider is ILocalMetadataProvider localInfo
                    ? await localInfo.GetMovieInfoByFileAsync(filePath, ct).ConfigureAwait(false) ?? await provider.GetMovieInfoAsync(movie, ct).ConfigureAwait(false)
                    : await provider.GetMovieInfoAsync(movie, ct).ConfigureAwait(false);
                var confidence = ComputeConfidence(baseConfidence, provider.Name);

                var result = new MatchResult(
                    MediaType.Movie,
                    confidence,
                    provider.Name,
                    Movie: movie,
                    MovieInfo: movieInfo);

                if (best is null || confidence > best.Confidence)
                    best = result;

                var threshold = IsLocalProvider(provider.Name) ? LocalConfidenceThreshold : OnlineConfidenceThreshold;
                if (confidence >= threshold)
                {
                    _logger.LogDebug("Provider chain short-circuit: {Provider} confidence={Confidence:F2}",
                        provider.Name, confidence);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Movie provider {Provider} failed in chain", provider.Name);
            }
        }

        return best ?? MatchResult.NoMatch(MediaType.Movie);
    }

    /// <summary>
    /// Match a file against episode providers in priority order.
    /// </summary>
    public async Task<MatchResult> MatchEpisodeAsync(string filePath, float baseConfidence, MediaType mediaType, CancellationToken ct = default)
    {
        using var activity = ActivitySrc.StartActivity("mediamatch.chain.episode");
        MatchResult? best = null;

        foreach (var provider in _episodeProviders)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                Episode? localEpisode = null;
                SeriesInfo? localSeriesInfo = null;

                if (provider is ILocalMetadataProvider local)
                {
                    localEpisode = await local.SearchEpisodeByFileAsync(filePath, ct).ConfigureAwait(false);
                    if (localEpisode is not null)
                        localSeriesInfo = await local.GetSeriesInfoByFileAsync(filePath, ct).ConfigureAwait(false);
                }

                if (localEpisode is not null)
                {
                    var confidence = IsLocalProvider(provider.Name) ? 0.95f : baseConfidence;
                    var result = new MatchResult(
                        mediaType,
                        confidence,
                        provider.Name,
                        Episode: localEpisode,
                        SeriesInfo: localSeriesInfo);

                    if (best is null || confidence > best.Confidence)
                        best = result;

                    var threshold = IsLocalProvider(provider.Name) ? LocalConfidenceThreshold : OnlineConfidenceThreshold;
                    if (confidence >= threshold)
                    {
                        _logger.LogDebug("Provider chain short-circuit: {Provider} confidence={Confidence:F2}",
                            provider.Name, confidence);
                        return result;
                    }

                    continue;
                }

                // Standard search for online providers
                var searchQuery = Path.GetFileNameWithoutExtension(filePath);
                var searchResults = await provider.SearchAsync(searchQuery, ct).ConfigureAwait(false);
                if (searchResults.Count == 0) continue;

                var series = searchResults[0];
                var seriesInfo = await provider.GetSeriesInfoAsync(series, ct).ConfigureAwait(false);

                var onlineResult = new MatchResult(
                    mediaType,
                    baseConfidence,
                    provider.Name,
                    SeriesInfo: seriesInfo);

                if (best is null || baseConfidence > best.Confidence)
                    best = onlineResult;

                if (baseConfidence >= OnlineConfidenceThreshold)
                    return onlineResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Episode provider {Provider} failed in chain", provider.Name);
            }
        }

        return best ?? MatchResult.NoMatch(mediaType);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static bool IsLocalProvider(string name) => LocalProviderNames.Contains(name);

    private static List<T> OrderProviders<T>(List<T> providers) where T : class
    {
        var local = new List<T>();
        var online = new List<T>();

        foreach (var p in providers)
        {
            var name = p switch
            {
                IMovieProvider mp => mp.Name,
                IEpisodeProvider ep => ep.Name,
                _ => string.Empty
            };

            if (IsLocalProvider(name))
                local.Add(p);
            else
                online.Add(p);
        }

        local.AddRange(online);
        return local;
    }

    private static float ComputeConfidence(float baseConfidence, string providerName)
    {
        if (IsLocalProvider(providerName))
            return Math.Min(baseConfidence + 0.30f, 0.95f);

        return baseConfidence;
    }
}
