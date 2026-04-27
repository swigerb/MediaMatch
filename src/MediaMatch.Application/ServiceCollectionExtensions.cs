using MediaMatch.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MediaMatch.Application;

/// <summary>
/// Extension methods for registering MediaMatch application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application-layer services: parallel file scanner, lazy metadata resolver.
    /// Call after <c>AddMediaMatchInfrastructure</c> to ensure dependencies are available.
    /// </summary>
    public static IServiceCollection AddMediaMatchApplication(this IServiceCollection services)
    {
        services.AddSingleton<IParallelFileScanner, Services.ParallelFileScanner>();
        services.AddSingleton<ILazyMetadataResolver, Services.LazyMetadataResolver>();

        return services;
    }
}
