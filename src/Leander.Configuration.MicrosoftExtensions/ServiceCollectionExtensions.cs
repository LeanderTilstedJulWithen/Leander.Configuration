using Leander.Configuration.MicrosoftExtensions.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leander.Configuration.MicrosoftExtensions;

/// <summary>
/// Registers a configuration contract, its snapshot and options built from it.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Reads <paramref name="configuration"/> with <paramref name="contract"/> immediately, and registers the contract
    /// and the snapshot as singletons.
    /// </summary>
    /// <remarks>
    /// An invalid configuration fails before the host is built. The snapshot is read once and never reloaded.
    /// </remarks>
    /// <exception cref="InvalidConfigurationException">The configuration doesn't satisfy the contract; lists every error.</exception>
    public static IServiceCollection AddConfigurationContract(
        this IServiceCollection services,
        ConfigurationContract contract,
        IConfiguration configuration) =>
        services.AddConfigurationContract(contract, configuration, ReadOptions.Default);

    /// <summary>
    /// Reads <paramref name="configuration"/> with <paramref name="contract"/> and <paramref name="options"/> immediately,
    /// and registers the contract and the snapshot as singletons.
    /// </summary>
    /// <remarks>
    /// An invalid configuration fails before the host is built. The snapshot is read once and never reloaded.
    /// </remarks>
    /// <exception cref="InvalidConfigurationException">The configuration doesn't satisfy the contract; lists every error.</exception>
    public static IServiceCollection AddConfigurationContract(
        this IServiceCollection services,
        ConfigurationContract contract,
        IConfiguration configuration,
        ReadOptions options)
    {
        var snapshot = contract.Read(configuration.AsValueSource(), options);

        services.AddSingleton(contract);
        services.AddSingleton(snapshot);
        return services;
    }

    /// <summary>
    /// Exposes <typeparamref name="T"/> as <see cref="IOptions{TOptions}"/>, <see cref="IOptionsSnapshot{TOptions}"/>
    /// and <see cref="IOptionsMonitor{TOptions}"/>, built from the snapshot by <paramref name="create"/>.
    /// </summary>
    /// <remarks>
    /// Requires <c>AddConfigurationContract</c>. The snapshot is already validated, so <c>ValidateOnStart()</c> is unnecessary.
    /// Every options name gets an instance built from the one snapshot.
    /// </remarks>
    public static IServiceCollection AddOptionsFrom<T>(
        this IServiceCollection services,
        Func<ConfigurationSnapshot, T> create)
        where T : class
    {
        services.AddOptions();
        services.AddSingleton<IOptionsFactory<T>>(provider =>
            new SnapshotOptionsFactory<T>(provider.GetRequiredService<ConfigurationSnapshot>(), create));
        return services;
    }
}
