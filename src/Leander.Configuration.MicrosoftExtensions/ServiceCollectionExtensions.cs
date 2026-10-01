using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// Exposes <typeparamref name="T"/> as <see cref="IOptions{TOptions}"/>, built once from the snapshot by <paramref name="create"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires a <see cref="ConfigurationSnapshot"/> in the container, e.g. from <c>AddConfigurationContract</c>.
    /// The snapshot is already validated, so <c>ValidateOnStart()</c> is unnecessary.
    /// </para>
    /// <para>
    /// The snapshot never reloads, so resolving <see cref="IOptionsSnapshot{TOptions}"/> or
    /// <see cref="IOptionsMonitor{TOptions}"/> of <typeparamref name="T"/> throws <see cref="InvalidOperationException"/>,
    /// instead of quietly returning values that never change. Your own registrations of them take precedence.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddOptionsFrom<T>(
        this IServiceCollection services,
        Func<ConfigurationSnapshot, T> create)
        where T : class
    {
        services.AddSingleton<IOptions<T>>(provider =>
            Options.Create(create(provider.GetRequiredService<ConfigurationSnapshot>())));

        // Without these, the host's open generic registrations would create T with its default factory: unbound, or failing.
        // TryAdd, so an application that provides them itself, e.g. from a snapshot it reloads, keeps its own.
        services.TryAddScoped<IOptionsSnapshot<T>>(_ => throw NotReloading<T>("IOptionsSnapshot"));
        services.TryAddSingleton<IOptionsMonitor<T>>(_ => throw NotReloading<T>("IOptionsMonitor"));
        return services;
    }

    private static InvalidOperationException NotReloading<T>(string service) =>
        new($"{service}<{typeof(T).Name}> is not supported: {typeof(T).Name} is built once from a configuration snapshot " +
            $"that never reloads. Use IOptions<{typeof(T).Name}>.");
}
