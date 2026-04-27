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
    /// Initialises a new <see cref="MetadataCache"/>.
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
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory();

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
    public void Remove(string key) => _cache.Remove(key);
}
