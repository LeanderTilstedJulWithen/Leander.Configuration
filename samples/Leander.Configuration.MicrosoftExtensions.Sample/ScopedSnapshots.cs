using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Leander.Configuration.MicrosoftExtensions.Sample;

// Singleton is the usual registration, not the only one. A scoped snapshot reads IConfiguration again in every
// scope, e.g. per request, so it sees changes to the configuration. A read can then fail in any scope, not only at start.
public static class ScopedSnapshots
{
    public static void Run()
    {
        Console.WriteLine("Scoped snapshots:");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Server:AllowedOrigins:0"] = "https://example.com" })
            .Build();

        using var services = new ServiceCollection()
            .AddScoped(_ => ServerContract.Contract.Read(configuration.AsValueSource(), ServerContract.ReadOptions))
            .BuildServiceProvider();

        ShowPort(services);
        configuration["Server:Port"] = "6000";
        ShowPort(services);

        Console.WriteLine();
    }

    // A new scope reads a new snapshot.
    private static void ShowPort(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var snapshot = scope.ServiceProvider.GetRequiredService<ConfigurationSnapshot>();
        Console.WriteLine($"  Server:Port     {snapshot.Get(ServerConfiguration.Port)}");
    }
}
