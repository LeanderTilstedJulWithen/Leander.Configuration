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
    /// Registers <paramref name="contract"/> and its snapshot as singletons. The snapshot is read from the container's
    /// <see cref="IConfiguration"/> with <see cref="ConfigurationContractOptions.ReadOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing is read here. With <see cref="ConfigurationContractOptions.ValidateOnStart"/>, the snapshot is read when
    /// the host starts, and an invalid configuration fails <c>StartAsync</c> with <see cref="InvalidConfigurationException"/>,
    /// which lists every error. Otherwise, or without a host, it is read on the first resolve.
    /// </para>
    /// <para>
    /// The snapshot is read once and never reloaded.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddConfigurationContract(
        this IServiceCollection services,
        ConfigurationContract contract,
        ConfigurationContractOptions? options = null)
    {
        options ??= ConfigurationContractOptions.Default;
        var readOptions = options.ReadOptions;

        services.AddSingleton(contract);
        services.AddSingleton(provider =>
            contract.Read(provider.GetRequiredService<IConfiguration>().AsValueSource(), readOptions));

        // Configuring StartupRead resolves the snapshot, which reads it. Through ValidateOnStart rather than our own
        // IStartupValidator: the host resolves only one, so ours would replace or be replaced by everyone else's.
        if (options.ValidateOnStart)
        {
            services.AddOptions<StartupRead>()
                .Configure<ConfigurationSnapshot>((_, _) => { })
                .ValidateOnStart();
        }

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

    // Options with nothing in them: only there to resolve the snapshot at start.
    private sealed class StartupRead;
}
