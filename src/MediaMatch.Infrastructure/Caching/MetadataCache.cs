using Microsoft.Extensions.Caching.Memory;

namespace MediaMatch.Infrastructure.Caching;

/// <summary>
/// Thread-safe in-memory cache for metadata results with configurable TTL.
/// Wraps <see cref="IMemoryCache"/> with a domain-specific API.
/// </summary>
public sealed class MetadataCache
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultTtl;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataCache"/> class.
    /// </summary>
    /// <param name="cache">The underlying memory cache.</param>
    /// <param name="defaultTtlMinutes">Default time-to-live in minutes.</param>
    public MetadataCache(IMemoryCache cache, int defaultTtlMinutes = 60)
    {
        _cache = cache;
        _defaultTtl = TimeSpan.FromMinutes(defaultTtlMinutes);
    }

    /// <summary>
    /// Gets a cached value or creates it asynchronously using the provided factory.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">An async factory to produce the value on cache miss.</param>
    /// <param name="ttl">Optional time-to-live override; uses the default TTL when <see langword="null"/>.</param>
    /// <returns>The cached or newly created value.</returns>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory().ConfigureAwait(false);

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl
        };

        _cache.Set(key, value, options);
        return value;
    }

    /// <summary>
    /// Removes a specific entry from the cache.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    public void Remove(string key) => _cache.Remove(key);
}
