using MediaMatch.Core.Configuration;
using MediaMatch.Core.Providers;
using MediaMatch.Core.Services;
using MediaMatch.Infrastructure.Actions;
using MediaMatch.Infrastructure.Caching;
using MediaMatch.Infrastructure.FileSystem;
using MediaMatch.Infrastructure.Http;
using MediaMatch.Infrastructure.Observability;
using MediaMatch.Infrastructure.Platform;
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
        ApiConfiguration? config = null,
        AniDbConfiguration? aniDbConfig = null,
        LlmConfiguration? llmConfig = null,
        AppSettings? appSettings = null)
    {
        var apiConfig = config ?? new ApiConfiguration();
        services.AddSingleton(apiConfig);

        var aniDbConf = aniDbConfig ?? new AniDbConfiguration();
        services.AddSingleton(aniDbConf);

        var llmConf = llmConfig ?? new LlmConfiguration();
        services.AddSingleton(llmConf);

        var settings = appSettings ?? new AppSettings();
        services.AddSingleton(settings);
        services.AddSingleton(settings.ApiKeys);
        services.AddSingleton(settings.Plex);
        services.AddSingleton(settings.Jellyfin);
        services.AddSingleton(settings.Performance);

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

        // Movie providers — local providers first for chain ordering
        services.AddSingleton<IMovieProvider, NfoMetadataProvider>();
        services.AddSingleton<IMovieProvider, XmlMetadataProvider>();
        services.AddSingleton<IMovieProvider, TmdbMovieProvider>();

        // Episode providers — local first, then online
        services.AddSingleton<IEpisodeProvider, NfoMetadataProvider>();
        services.AddSingleton<IEpisodeProvider, XmlMetadataProvider>();
        services.AddSingleton<IEpisodeProvider, TmdbEpisodeProvider>();
        services.AddSingleton<IEpisodeProvider, TvdbEpisodeProvider>();
        // AniDB provider — uses its own HttpClient for XML + dedicated rate limiting
        services.AddSingleton<IEpisodeProvider, AniDbProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("AniDB");
            return new AniDbProvider(
                httpClient,
                sp.GetRequiredService<MetadataCache>(),
                aniDbConf,
                sp.GetRequiredService<ILogger<AniDbProvider>>());
        });
        services.AddSingleton<IAniDbProvider>(sp =>
            sp.GetServices<IEpisodeProvider>().OfType<AniDbProvider>().First());

        // AniDB-TVDb mapping provider for fallback lookups
        services.AddSingleton<AniDbTvdbMappingProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("AniDBMapping");
            return new AniDbTvdbMappingProvider(
                httpClient,
                sp.GetRequiredService<MetadataCache>(),
                aniDbConf,
                sp.GetServices<IEpisodeProvider>(),
                sp.GetRequiredService<ILogger<AniDbTvdbMappingProvider>>());
        });

        // Artwork providers
        services.AddSingleton<IArtworkProvider, TmdbArtworkProvider>();

        // Subtitle providers
        services.AddSingleton<ISubtitleProvider, OpenSubtitlesProvider>();

        // LLM providers for AI-assisted renaming
        services.AddSingleton<ILlmProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("OpenAI");
            httpClient.Timeout = TimeSpan.FromSeconds(llmConf.TimeoutSeconds);
            return new OpenAiProvider(httpClient, llmConf, sp.GetRequiredService<ILogger<OpenAiProvider>>());
        });
        services.AddSingleton<ILlmProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("AzureOpenAI");
            httpClient.Timeout = TimeSpan.FromSeconds(llmConf.TimeoutSeconds);
            return new AzureOpenAiProvider(httpClient, llmConf, sp.GetRequiredService<ILogger<AzureOpenAiProvider>>());
        });
        services.AddSingleton<ILlmProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("Ollama");
            httpClient.Timeout = TimeSpan.FromSeconds(llmConf.TimeoutSeconds);
            return new OllamaProvider(httpClient, llmConf, sp.GetRequiredService<ILogger<OllamaProvider>>());
        });

        // File clone services (Windows-only, suppressed for cross-platform TFM)
#pragma warning disable CA1416
        services.AddSingleton<ReFsCloneHandler>();
        services.AddSingleton<HardLinkHandler>();
        services.AddSingleton<FileCloneService>();
#pragma warning restore CA1416

        // Music providers
        services.AddSingleton<IMusicProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("MusicBrainz");
            return new MusicBrainzProvider(httpClient, sp.GetRequiredService<ILogger<MusicBrainzProvider>>());
        });
        services.AddSingleton<IMusicProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("AcoustID");
            return new AcoustIdProvider(httpClient, settings.ApiKeys, sp.GetRequiredService<ILogger<AcoustIdProvider>>());
        });

        // Post-process actions
        services.AddSingleton<IPostProcessAction>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new PlexRefreshAction(
                factory.CreateClient("Plex"),
                sp.GetRequiredService<PlexSettings>(),
                sp.GetRequiredService<ILogger<PlexRefreshAction>>());
        });
        services.AddSingleton<IPostProcessAction>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new JellyfinRefreshAction(
                factory.CreateClient("Jellyfin"),
                sp.GetRequiredService<JellyfinSettings>(),
                sp.GetRequiredService<ILogger<JellyfinRefreshAction>>());
        });
        services.AddSingleton<IPostProcessAction, ThumbnailGenerateAction>();

        // Network path detection (Windows P/Invoke)
#pragma warning disable CA1416
        services.AddSingleton<INetworkPathDetector, NetworkPathDetector>();
#pragma warning restore CA1416

        // Platform detection
        services.AddSingleton<IPlatformService, PlatformService>();

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