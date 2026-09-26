using Leander.Configuration;
using Leander.Configuration.MicrosoftExtensions;
using Leander.Configuration.MicrosoftExtensions.Sample;
using Microsoft.Extensions.Configuration;

// Microsoft's providers own the sources. Later providers override earlier ones, so try e.g.:
//   dotnet run -- Server:Port=99999
//   dotnet run -- Server:AllowedOrigins:1="not a uri"
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

// The contract is built once at startup. It fails fast if a primitive cannot be resolved or a key is defined twice.
var contract = new ConfigurationContractBuilder()
    .RegisterDefaultPrimitives()
    .Register(ServerConfiguration.Host)
    .Register(ServerConfiguration.Port)
    .Register(ServerConfiguration.AllowedOrigins)
    .Register(ServerConfiguration.Features)
    .Build();

try
{
    var snapshot = contract.Read(configuration.AsValueSource());

    Console.WriteLine($"Host            {snapshot.Get(ServerConfiguration.Host)}");
    Console.WriteLine($"Port            {snapshot.Get(ServerConfiguration.Port)}");
    Console.WriteLine($"AllowedOrigins  {string.Join(", ", snapshot.Get(ServerConfiguration.AllowedOrigins))}");
    Console.WriteLine($"Features        {string.Join(", ", snapshot.Get(ServerConfiguration.Features))}");
}
catch (InvalidConfigurationException exception)
{
    Console.WriteLine(exception.Message);
    return 1;
}

return 0;
