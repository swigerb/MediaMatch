using MediaMatch.Core.Configuration;
using MediaMatch.Core.Services;
using MediaMatch.Infrastructure.Unix.FileSystem;
using MediaMatch.Infrastructure.Unix.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace MediaMatch.Infrastructure.Unix;

/// <summary>
/// Registers Unix/macOS-specific infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Unix/macOS implementations of platform-specific services.
    /// </summary>
    public static IServiceCollection AddUnixInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsEncryption, AesFileEncryption>();
        services.AddSingleton<IHardLinkHandler, UnixHardLinkHandler>();
        services.AddSingleton<INetworkPathDetector, UnixNetworkPathDetector>();
        services.AddSingleton<IFileCloneService, CopyFallbackCloneService>();

        return services;
    }
}
