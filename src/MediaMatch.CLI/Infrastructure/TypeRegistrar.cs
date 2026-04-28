using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace MediaMatch.CLI.Infrastructure;

/// <summary>
/// Bridges Microsoft.Extensions.DependencyInjection with Spectre.Console.Cli.
/// </summary>
internal sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="services">The service collection to register types into.</param>
    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    /// <inheritdoc/>
    public ITypeResolver Build() =>
        new TypeResolver(_services.BuildServiceProvider());

    /// <inheritdoc/>
    public void Register(Type service, Type implementation) =>
        _services.AddSingleton(service, implementation);

    /// <inheritdoc/>
    public void RegisterInstance(Type service, object implementation) =>
        _services.AddSingleton(service, implementation);

    /// <inheritdoc/>
    public void RegisterLazy(Type service, Func<object> factory) =>
        _services.AddSingleton(service, _ => factory());
}

/// <summary>
/// Resolves types from the built <see cref="ServiceProvider"/>.
/// </summary>
internal sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly ServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolver"/> class.
    /// </summary>
    /// <param name="provider">The built service provider.</param>
    public TypeResolver(ServiceProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc/>
    public object? Resolve(Type? type) =>
        type is null ? null : _provider.GetService(type);

    /// <inheritdoc/>
    public void Dispose() => _provider.Dispose();
}
