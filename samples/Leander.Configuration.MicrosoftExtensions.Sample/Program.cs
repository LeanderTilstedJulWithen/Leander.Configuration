using Leander.Configuration;
using Leander.Configuration.MicrosoftExtensions;
using Leander.Configuration.MicrosoftExtensions.Sample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

// The host's providers own the sources: appsettings.json, environment variables, then command-line arguments.
// Later providers override earlier ones, so try e.g.:
//   dotnet run -- Server:Port=99999
//   dotnet run -- Server:AllowedOrigins:1="not a uri"
//   dotnet run -- Server:CertificatePath=cert.pfx
//   dotnet run -- Server:Prot=8080
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// The contract is built once at startup. It fails fast if a key is defined twice or two primitives share a name.
var contract = new ConfigurationContractBuilder()
    .Register(ServerConfiguration.Host)
    .Register(ServerConfiguration.Port)
    .Register(ServerConfiguration.AllowedOrigins)
    .Register(ServerConfiguration.Features)
    .Register(ServerConfiguration.CertificatePath)
    .Build();

// The configuration is read from the host's IConfiguration when the host starts.
// Server belongs to this application, so a key there that the contract doesn't define is a mistake, e.g. Server:Prot.
// The rest of the configuration (Logging, environment variables, ...) isn't checked.
var options = new ConfigurationContractOptions
{
    ReadOptions = new ReadOptions { CheckedSections = ["Server"], WarningsAsErrors = true },
};

builder.Services
    .AddConfigurationContract(contract, options)
    .AddOptionsFrom(ServerOptions.FromSnapshot);

using var host = builder.Build();

// An invalid configuration fails here, before anything runs.
try
{
    await host.StartAsync();
}
catch (InvalidConfigurationException exception)
{
    Console.WriteLine(exception.Message);
    return 1;
}

// Consumers see ordinary IOptions<T>.
var server = host.Services.GetRequiredService<IOptions<ServerOptions>>().Value;

Console.WriteLine($"Host            {server.Host}");
Console.WriteLine($"Port            {server.Port}");
Console.WriteLine($"AllowedOrigins  {string.Join(", ", server.AllowedOrigins)}");
Console.WriteLine($"Features        {string.Join(", ", server.Features)}");
Console.WriteLine($"Certificate     {server.CertificatePath ?? "(none, plain HTTP)"}");

await host.StopAsync();
return 0;
