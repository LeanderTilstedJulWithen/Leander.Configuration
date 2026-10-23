using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leander.Configuration.MicrosoftExtensions.Sample;

// With ReloadOnChange, the contract is read again whenever IConfiguration reloads, e.g. when appsettings.json is saved.
// IOptionsMonitor<T> and IOptionsSnapshot<T> follow it. A reload is all or nothing: an invalid configuration keeps the
// last valid values and goes to OnReloadFailed.
public static class Reloading
{
    public static void Run()
    {
        Console.WriteLine("Reloading:");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Server:AllowedOrigins:0"] = "https://example.com" })
            .Build();

        using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddConfigurationContract(
                ServerContract.Contract,
                new ConfigurationContractOptions { ReadOptions = ServerContract.ReadOptions, ReloadOnChange = true })
            .AddOptionsFrom(CorsOptions.FromSnapshot)
            .BuildServiceProvider();

        // No logging dependency: subscribe once to find out about invalid reloads.
        var monitor = services.GetRequiredService<ConfigurationSnapshotMonitor>();
        monitor.OnReloadFailed(exception =>
        {
            foreach (var diagnostic in exception.Diagnostics)
            {
                Console.WriteLine($"  Reload failed: {diagnostic}");
            }
        });

        var cors = services.GetRequiredService<IOptionsMonitor<CorsOptions>>();
        cors.OnChange((options, _) => Console.WriteLine($"  Changed:        {string.Join(", ", options.AllowedOrigins)}"));
        Console.WriteLine($"  AllowedOrigins  {string.Join(", ", cors.CurrentValue.AllowedOrigins)}");

        // In a host, the JSON file provider reloads by itself. In memory, set values and reload by hand.
        configuration["Server:AllowedOrigins:1"] = "https://example.org";
        configuration.Reload();

        configuration["Server:Port"] = "99999";
        configuration.Reload();
        Console.WriteLine($"  Server:Port     {monitor.Current.Get(ServerConfiguration.Port)} (kept)");

        Console.WriteLine();
    }
}
