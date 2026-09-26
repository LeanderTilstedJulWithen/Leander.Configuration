using Leander.Configuration.MicrosoftExtensions.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leander.Configuration.MicrosoftExtensions;

public static class ServiceCollectionExtensions
{
    // Reads the configuration immediately, so an invalid configuration fails before the host is built,
    // with InvalidConfigurationException listing every problem. The snapshot is read once and never reloaded.
    // Registers the contract and the snapshot as singletons.
    public static IServiceCollection AddConfigurationContract(
        this IServiceCollection services,
        ConfigurationContract contract,
        IConfiguration configuration)
    {
        var snapshot = contract.Read(configuration.AsValueSource());

        services.AddSingleton(contract);
        services.AddSingleton(snapshot);
        return services;
    }

    // Exposes T as IOptions<T>, IOptionsSnapshot<T> and IOptionsMonitor<T>, built from the snapshot by create.
    // Requires AddConfigurationContract. The snapshot is already validated, so ValidateOnStart() is unnecessary.
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
