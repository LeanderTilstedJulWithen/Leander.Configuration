using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Leander.Configuration.MicrosoftExtensions.Sample;

// The usual registration: one snapshot, read from the host's IConfiguration when the host starts.
// The host's providers own the sources: appsettings.json, environment variables, then command-line arguments.
// Later providers override earlier ones, so try e.g.:
//   dotnet run -- Server:Port=99999
//   dotnet run -- Server:AllowedOrigins:1="not a uri"
//   dotnet run -- Server:CertificatePath=cert.pfx
//   dotnet run -- Server:Prot=8080
public static class HostRegistration
{
    public static async Task<int> RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.Services
            .AddConfigurationContract(ServerContract.Contract, new ConfigurationContractOptions { ReadOptions = ServerContract.ReadOptions })
            // Without IOptions: the options record itself is the service, built once from the snapshot.
            .AddSingleton(provider => ServerOptions.FromSnapshot(provider.GetRequiredService<ConfigurationSnapshot>()))
            // With IOptions: for code that expects IOptions<T>.
            .AddOptionsFrom(CorsOptions.FromSnapshot)
            .AddSingleton<Server>();

        using var host = builder.Build();

        // An invalid configuration fails here, before anything runs. The message lists every problem.
        try
        {
            await host.StartAsync();
        }
        catch (InvalidConfigurationException exception)
        {
            Console.WriteLine(exception.Message);
            return 1;
        }

        Console.WriteLine("Registered with the host:");
        host.Services.GetRequiredService<Server>().Describe();

        await host.StopAsync();
        return 0;
    }
}
