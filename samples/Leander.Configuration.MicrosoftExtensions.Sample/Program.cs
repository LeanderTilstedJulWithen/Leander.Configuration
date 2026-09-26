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
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// The contract is built once at startup. It fails fast if a primitive cannot be resolved or a key is defined twice.
var contract = new ConfigurationContractBuilder()
    .RegisterDefaultPrimitives()
    .Register(ServerConfiguration.Host)
    .Register(ServerConfiguration.Port)
    .Register(ServerConfiguration.AllowedOrigins)
    .Register(ServerConfiguration.Features)
    .Build();

// The configuration is read here, before the host is built. An invalid configuration never reaches the services.
try
{
    builder.Services
        .AddConfigurationContract(contract, builder.Configuration)
        .AddOptionsFrom(ServerOptions.From);
}
catch (InvalidConfigurationException exception)
{
    Console.WriteLine(exception.Message);
    return 1;
}

using var host = builder.Build();

// Consumers see ordinary IOptions<T>.
var options = host.Services.GetRequiredService<IOptions<ServerOptions>>().Value;

Console.WriteLine($"Host            {options.Host}");
Console.WriteLine($"Port            {options.Port}");
Console.WriteLine($"AllowedOrigins  {string.Join(", ", options.AllowedOrigins)}");
Console.WriteLine($"Features        {string.Join(", ", options.Features)}");

return 0;
