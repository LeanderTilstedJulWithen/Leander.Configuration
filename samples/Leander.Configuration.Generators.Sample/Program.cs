using Leander.Configuration;
using Leander.Configuration.Generators.Sample;
using Leander.Configuration.MicrosoftExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

// Registration through attributes: every definition marked with RegisterConfiguration in this assembly, found through reflection.
// Explicit Register(...) calls stay the default; this is opt-in, and can be mixed with them.
// Try e.g.:
//   dotnet run -- Server:Port=99999
//   dotnet run -- Database:CommandTimeout=0
var contract = new ConfigurationContractBuilder()
    .RegisterFrom(typeof(Program).Assembly)
    .Build();

Console.WriteLine("Registered through attributes:");
foreach (var definition in contract.Definitions)
{
    Console.WriteLine($"  {definition.Key}");
}

Console.WriteLine();

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// FromSnapshot is generated: the explicit construction, from the validated snapshot.
builder.Services
    .AddConfigurationContract(contract)
    .AddSingleton(provider => ServerOptions.FromSnapshot(provider.GetRequiredService<ConfigurationSnapshot>()))
    .AddOptionsFrom(DatabaseOptions.FromSnapshot);

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

var server = host.Services.GetRequiredService<ServerOptions>();
var database = host.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

Console.WriteLine("Options built by the generated code:");
Console.WriteLine($"  Host            {server.Host}");
Console.WriteLine($"  Port            {server.Port}");
Console.WriteLine($"  AllowedOrigins  {string.Join(", ", server.AllowedOrigins)}");
Console.WriteLine($"  Certificate     {server.CertificatePath ?? "(none, plain HTTP)"}");
Console.WriteLine($"  CommandTimeout  {database.CommandTimeout}");
Console.WriteLine($"  Connection      {(database.ConnectionString.Length > 0 ? "(set)" : "(empty)")}");

await host.StopAsync();
return 0;
