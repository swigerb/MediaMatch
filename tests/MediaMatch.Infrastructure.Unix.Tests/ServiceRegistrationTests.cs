using FluentAssertions;
using MediaMatch.Core.Configuration;
using MediaMatch.Core.Services;
using MediaMatch.Infrastructure.Unix.FileSystem;
using MediaMatch.Infrastructure.Unix.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace MediaMatch.Infrastructure.Unix.Tests;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddUnixInfrastructure_RegistersAllServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUnixInfrastructure();

        var provider = services.BuildServiceProvider();

        provider.GetService<ISettingsEncryption>().Should().NotBeNull()
            .And.BeOfType<AesFileEncryption>();
        provider.GetService<IHardLinkHandler>().Should().NotBeNull()
            .And.BeOfType<UnixHardLinkHandler>();
        provider.GetService<INetworkPathDetector>().Should().NotBeNull()
            .And.BeOfType<UnixNetworkPathDetector>();
        provider.GetService<IFileCloneService>().Should().NotBeNull()
            .And.BeOfType<CopyFallbackCloneService>();
    }
}
