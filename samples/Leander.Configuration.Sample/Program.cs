using Leander.Configuration;
using Leander.Configuration.Sample;

// The contract is built once at startup. It fails fast if a primitive cannot be resolved or a key is defined twice.
var contract = new ConfigurationContractBuilder()
    .RegisterDefaultPrimitives()
    .Register(ServerConfiguration.Host)
    .Register(ServerConfiguration.Port)
    .Register(ServerConfiguration.RequestTimeout)
    .Register(ServerConfiguration.AllowedOrigins)
    .Register(ServerConfiguration.Features)
    .Register(ServerConfiguration.AdminEmail)
    .Register(ServerConfiguration.Verbosity)
    .Build();

// In a real application these values come from IConfiguration (appsettings.json, environment variables, ...).
var valid = ValueSource.FromPairs(new Dictionary<string, string?>
{
    ["Server:Host"] = "example.com",
    ["Server:AllowedOrigins:0"] = "https://example.com",
    ["Server:AllowedOrigins:1"] = "https://admin.example.com",
    ["Server:Features"] = "search, export",
    ["Admin:Email"] = "  ops@example.com ",
    ["Logging:Verbosity"] = "verbose",
});

var invalid = ValueSource.FromPairs(new Dictionary<string, string?>
{
    ["Server:Port"] = "99999",
    ["Server:RequestTimeout"] = "30s",
    ["Server:AllowedOrigins:0"] = "https://example.com",
    ["Server:AllowedOrigins:1"] = "not a uri",
    ["Admin:Email"] = "ops.example.com",
    ["Logging:Verbosity"] = "Chatty",
});

Console.WriteLine("Valid configuration:");
var options = ServerOptions.From(contract.Read(valid));
Console.WriteLine($"  Host            {options.Host}");
Console.WriteLine($"  Port            {options.Port}");
Console.WriteLine($"  RequestTimeout  {options.RequestTimeout}");
Console.WriteLine($"  AllowedOrigins  {string.Join(", ", options.AllowedOrigins)}");
Console.WriteLine($"  Features        {string.Join(", ", options.Features)}");
Console.WriteLine($"  AdminEmail      {options.AdminEmail}");
Console.WriteLine($"  Verbosity       {options.Verbosity}");
Console.WriteLine();

Console.WriteLine("Invalid configuration:");
try
{
    contract.Read(invalid);
}
catch (InvalidConfigurationException exception)
{
    Console.WriteLine(exception.Message);
}
