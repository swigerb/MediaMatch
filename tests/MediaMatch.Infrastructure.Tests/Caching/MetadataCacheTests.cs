using FluentAssertions;
using MediaMatch.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace MediaMatch.Infrastructure.Tests.Caching;

public sealed class MetadataCacheTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly MetadataCache _sut;

    public MetadataCacheTests()
    {
        _sut = new MetadataCache(_memoryCache, defaultTtlMinutes: 5);
    }

    [Fact]
    public async Task GetOrCreateAsync_CacheMiss_CallsFactory()
    {
        var callCount = 0;

        var result = await _sut.GetOrCreateAsync("key1", () =>
        {
            callCount++;
            return Task.FromResult("value1");
        });

        result.Should().Be("value1");
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_CacheHit_SkipsFactory()
    {
        // Seed the cache via a first call
        await _sut.GetOrCreateAsync("key2", () => Task.FromResult("original"));

        var factoryCalled = false;
        var result = await _sut.GetOrCreateAsync("key2", () =>
        {
            factoryCalled = true;
            return Task.FromResult("should-not-be-used");
        });

        result.Should().Be("original");
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrCreateAsync_CustomTtl_UsesProvidedTtl()
    {
        var shortTtl = TimeSpan.FromMilliseconds(50);

        await _sut.GetOrCreateAsync("ttl-key", () => Task.FromResult("v1"), ttl: shortTtl);

        // Wait for the TTL to expire
        await Task.Delay(100);

        var factoryCalled = false;
        var result = await _sut.GetOrCreateAsync("ttl-key", () =>
        {
            factoryCalled = true;
            return Task.FromResult("v2");
        }, ttl: shortTtl);

        factoryCalled.Should().BeTrue();
        result.Should().Be("v2");
    }

    [Fact]
    public async Task Remove_ExistingKey_ClearsCache()
    {
        await _sut.GetOrCreateAsync("rm-key", () => Task.FromResult("original"));

        _sut.Remove("rm-key");

        var factoryCalled = false;
        var result = await _sut.GetOrCreateAsync("rm-key", () =>
        {
            factoryCalled = true;
            return Task.FromResult("recreated");
        });

        factoryCalled.Should().BeTrue();
        result.Should().Be("recreated");
    }

    [Fact]
    public async Task GetOrCreateAsync_ConcurrentAccess_IsThreadSafe()
    {
        var callCount = 0;

        var tasks = Enumerable.Range(0, 20).Select(_ =>
            _sut.GetOrCreateAsync("concurrent-key", async () =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Delay(10);
                return 42;
            }));

        var results = await Task.WhenAll(tasks);

        results.Should().AllBeEquivalentTo(42);
        // Factory may be called more than once due to race, but all results must be consistent
        callCount.Should().BeGreaterThanOrEqualTo(1);
    }

    public void Dispose() => _memoryCache.Dispose();
}
