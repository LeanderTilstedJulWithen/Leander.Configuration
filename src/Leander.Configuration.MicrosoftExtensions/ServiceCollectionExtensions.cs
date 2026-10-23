using Leander.Configuration.MicrosoftExtensions.Internal;
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
    /// Registers <paramref name="contract"/>, its snapshot and a <see cref="ConfigurationSnapshotMonitor"/> as singletons.
    /// The snapshot is read from the container's <see cref="IConfiguration"/> with <see cref="ConfigurationContractOptions.ReadOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing is read here. With <see cref="ConfigurationContractOptions.ValidateOnStart"/>, the snapshot is read when
    /// the host starts, and an invalid configuration fails <c>StartAsync</c> with <see cref="InvalidConfigurationException"/>,
    /// which lists every error. Otherwise, or without a host, it is read on the first resolve.
    /// </para>
    /// <para>
    /// The <see cref="ConfigurationSnapshot"/> service is the snapshot read at startup, and never changes. With
    /// <see cref="ConfigurationContractOptions.ReloadOnChange"/>, <see cref="ConfigurationSnapshotMonitor.Current"/> follows
    /// the configuration.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddConfigurationContract(
        this IServiceCollection services,
        ConfigurationContract contract,
        ConfigurationContractOptions? options = null)
    {
        options ??= ConfigurationContractOptions.Default;

        services.AddSingleton(contract);
        services.AddSingleton(provider =>
            new ConfigurationSnapshotMonitor(contract, provider.GetRequiredService<IConfiguration>(), options));
        services.AddSingleton(provider => provider.GetRequiredService<ConfigurationSnapshotMonitor>().Startup);
        services.TryAddScoped<ScopeSnapshot>();

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
    /// Exposes <typeparamref name="T"/> as <see cref="IOptions{TOptions}"/>, built from the snapshot by <paramref name="create"/>,
    /// and with <see cref="ConfigurationContractOptions.ReloadOnChange"/>, as <see cref="IOptionsMonitor{TOptions}"/> and
    /// <see cref="IOptionsSnapshot{TOptions}"/> too.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires a <see cref="ConfigurationSnapshot"/> in the container, e.g. from <c>AddConfigurationContract</c>.
    /// The snapshot is already validated, so <c>ValidateOnStart()</c> is unnecessary.
    /// </para>
    /// <para>
    /// <typeparamref name="T"/> is built at most once per snapshot. <see cref="IOptions{TOptions}"/> is built from the snapshot
    /// read at startup, <see cref="IOptionsMonitor{TOptions}"/> from <see cref="ConfigurationSnapshotMonitor.Current"/>, and
    /// <see cref="IOptionsSnapshot{TOptions}"/> from the snapshot that was current when the scope first asked for options,
    /// so every options type in a scope comes from the same read. Only unnamed options are supported.
    /// </para>
    /// <para>
    /// Without <see cref="ConfigurationContractOptions.ReloadOnChange"/>, resolving <see cref="IOptionsSnapshot{TOptions}"/> or
    /// <see cref="IOptionsMonitor{TOptions}"/> of <typeparamref name="T"/> throws <see cref="InvalidOperationException"/>,
    /// instead of quietly returning values that never change. Your own registrations of them take precedence.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddOptionsFrom<T>(
        this IServiceCollection services,
        Func<ConfigurationSnapshot, T> create)
        where T : class
    {
        services.AddSingleton(new SnapshotOptions<T>(create));
        services.AddSingleton<IOptions<T>>(provider =>
            Options.Create(provider.GetRequiredService<SnapshotOptions<T>>().Get(provider.GetRequiredService<ConfigurationSnapshot>())));

        // Without these, the host's open generic registrations would create T with its default factory: unbound, or failing.
        // TryAdd, so an application that provides them itself keeps its own.
        services.TryAddScoped<IOptionsSnapshot<T>>(provider =>
            new ScopedOptionsSnapshot<T>(
                Reloading<T>(provider, "IOptionsSnapshot").GetRequiredService<ScopeSnapshot>(),
                provider.GetRequiredService<SnapshotOptions<T>>()));
        services.TryAddSingleton<IOptionsMonitor<T>>(provider =>
            new ReloadingOptionsMonitor<T>(
                Reloading<T>(provider, "IOptionsMonitor").GetRequiredService<ConfigurationSnapshotMonitor>(),
                provider.GetRequiredService<SnapshotOptions<T>>()));
        return services;
    }

    // The provider, once it is known that the contract reloads.
    private static IServiceProvider Reloading<T>(IServiceProvider provider, string service) =>
        provider.GetRequiredService<ConfigurationSnapshotMonitor>().ReloadsOnChange
            ? provider
            : throw new InvalidOperationException(
                $"{service}<{typeof(T).Name}> is not supported: {typeof(T).Name} is built from a configuration snapshot that " +
                $"never reloads. Use IOptions<{typeof(T).Name}>, or turn on {nameof(ConfigurationContractOptions)}." +
                $"{nameof(ConfigurationContractOptions.ReloadOnChange)}.");

    // Options with nothing in them: only there to resolve the snapshot at start.
    private sealed class StartupRead;
}
