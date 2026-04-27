using System.Collections.Concurrent;
using MediaMatch.Core.Models;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Application.Services;

/// <summary>
/// Deferred metadata resolver that only fetches metadata when explicitly
/// requested. Caches results per session to avoid duplicate API calls.
/// </summary>
public sealed class LazyMetadataResolver : ILazyMetadataResolver
{
    private readonly IEnumerable<IMovieProvider> _movieProviders;
    private readonly IEnumerable<IEpisodeProvider> _episodeProviders;
    private readonly ILogger<LazyMetadataResolver> _logger;

    // Pending registrations: filePath → (cleanTitle, year)
    private readonly ConcurrentDictionary<string, (string CleanTitle, int? Year)> _pending = new(StringComparer.OrdinalIgnoreCase);

    // Session caches
    private readonly ConcurrentDictionary<string, IReadOnlyList<Movie>> _movieCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IReadOnlyList<SearchResult>> _episodeCache = new(StringComparer.OrdinalIgnoreCase);

    public LazyMetadataResolver(
        IEnumerable<IMovieProvider> movieProviders,
        IEnumerable<IEpisodeProvider> episodeProviders,
        ILogger<LazyMetadataResolver>? logger = null)
    {
        _movieProviders = movieProviders;
        _episodeProviders = episodeProviders;
        _logger = logger ?? NullLogger<LazyMetadataResolver>.Instance;
    }

    public void Register(string filePath, string cleanTitle, int? year = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _pending[filePath] = (cleanTitle ?? string.Empty, year);
        _logger.LogDebug("Registered lazy metadata for {FilePath}: title={Title}, year={Year}", filePath, cleanTitle, year);
    }

    public async Task<IReadOnlyList<Movie>> ResolveMovieAsync(string filePath, CancellationToken ct = default)
    {
        if (_movieCache.TryGetValue(filePath, out var cached))
            return cached;

        if (!_pending.TryGetValue(filePath, out var registration))
        {
            _logger.LogWarning("No registration found for {FilePath}", filePath);
            return [];
        }

        var results = new List<Movie>();
        foreach (var provider in _movieProviders)
        {
            try
            {
                var movies = await provider.SearchAsync(registration.CleanTitle, registration.Year, ct);
                results.AddRange(movies);
                if (results.Count > 0) break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Movie provider {Provider} failed for {FilePath}", provider.Name, filePath);
            }
        }

        var readOnlyResults = (IReadOnlyList<Movie>)results;
        _movieCache[filePath] = readOnlyResults;
        return readOnlyResults;
    }

    public async Task<IReadOnlyList<SearchResult>> ResolveEpisodeSearchAsync(string filePath, CancellationToken ct = default)
    {
        if (_episodeCache.TryGetValue(filePath, out var cached))
            return cached;

        if (!_pending.TryGetValue(filePath, out var registration))
        {
            _logger.LogWarning("No registration found for {FilePath}", filePath);
            return [];
        }

        var results = new List<SearchResult>();
        foreach (var provider in _episodeProviders)
        {
            try
            {
                var searchResults = await provider.SearchAsync(registration.CleanTitle, ct);
                results.AddRange(searchResults);
                if (results.Count > 0) break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Episode provider {Provider} failed for {FilePath}", provider.Name, filePath);
            }
        }

        var readOnlyResults = (IReadOnlyList<SearchResult>)results;
        _episodeCache[filePath] = readOnlyResults;
        return readOnlyResults;
    }

    public void ClearCache()
    {
        _movieCache.Clear();
        _episodeCache.Clear();
        _pending.Clear();
        _logger.LogDebug("Lazy metadata cache cleared");
    }
}
