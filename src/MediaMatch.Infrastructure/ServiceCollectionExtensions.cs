using Microsoft.Extensions.DependencyInjection;

namespace MediaMatch.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediaMatchInfrastructure(this IServiceCollection services)
    {
        // Register HttpClientFactory
        services.AddHttpClient();
        
        // Register memory cache
        services.AddMemoryCache();
        
        // Provider registrations will be added here as we implement them
        // services.AddSingleton<IMovieProvider, TmdbMovieProvider>();
        // services.AddSingleton<IEpisodeProvider, TvdbSeriesProvider>();
        
        return services;
    }
}
