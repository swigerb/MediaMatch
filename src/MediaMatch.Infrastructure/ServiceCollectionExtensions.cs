using MediaMatch.Core.Configuration;
using MediaMatch.Core.Providers;
using MediaMatch.Infrastructure.Caching;
using MediaMatch.Infrastructure.Http;
using MediaMatch.Infrastructure.Observability;
using MediaMatch.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediaMatch.Infrastructure;

/// <summary>
/// Extension methods for registering MediaMatch infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all MediaMatch infrastructure services including
    /// HTTP clients, caching, metadata providers, and telemetry.
    /// </summary>
    public static IServiceCollection AddMediaMatchInfrastructure(
        this IServiceCollection services,
        ApiConfiguration? config = null)
    {
        var apiConfig = config ?? new ApiConfiguration();
        services.AddSingleton(apiConfig);

        // Register HttpClientFactory with named clients
        services.AddHttpClient();

        // Register memory cache
        services.AddMemoryCache();

        // Logging (Microsoft.Extensions.Logging abstractions)
        services.AddLogging();

        // HTTP infrastructure
        services.AddSingleton<MediaMatchHttpClient>();

        // Metadata cache
        services.AddSingleton<MetadataCache>(sp =>
        {
            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            return new MetadataCache(cache, apiConfig.CacheTtlMinutes);
        });

        // Movie providers
        services.AddSingleton<IMovieProvider, TmdbMovieProvider>();

        // Episode providers — register both; consumers can choose by Name
        services.AddSingleton<IEpisodeProvider, TmdbEpisodeProvider>();
        services.AddSingleton<IEpisodeProvider, TvdbEpisodeProvider>();

        // Artwork providers
        services.AddSingleton<IArtworkProvider, TmdbArtworkProvider>();

        // Subtitle providers
        services.AddSingleton<ISubtitleProvider, OpenSubtitlesProvider>();

        return services;
    }

    /// <summary>
    /// Registers OpenTelemetry and Serilog observability services.
    /// Call this in addition to <see cref="AddMediaMatchInfrastructure"/>
    /// for full telemetry support.
    /// </summary>
    public static IServiceCollection AddMediaMatchTelemetry(
        this IServiceCollection services,
        bool enableConsole = true,
        bool debugMode = false)
    {
        // Initialize Serilog as the global logger
        SerilogConfig.Initialize(enableConsole, debugMode);

        return services;
    }
}