using Leander.Configuration;
using Leander.Configuration.Sample;
using Leander.Configuration.Tooling;

// The contract is built once at startup. It fails fast if a key is defined twice or two primitives share a name.
// Named primitives are described once in the documentation and linked from the keys that use them.
var contract = new ConfigurationContractBuilder()
    .Register(ServerConfiguration.Host)
    .Register(ServerConfiguration.Port)
    .Register(ServerConfiguration.RequestTimeout)
    .Register(ServerConfiguration.MaxConnections)
    .Register(ServerConfiguration.AllowedOrigins)
    .Register(ServerConfiguration.Features)
    .Register(ServerConfiguration.AdminEmail)
    .Register(ServerConfiguration.BackupEmail)
    .Register(ServerConfiguration.ApiKey)
    .Register(ServerConfiguration.Verbosity)
    .Build();

// In a real application these values come from IConfiguration (appsettings.json, environment variables, ...).
// Server:MaxConnections is missing, so it's null.
var validPairs = new Dictionary<string, string?>
{
    ["Server:Host"] = "example.com",
    ["Server:AllowedOrigins:0"] = "https://example.com",
    ["Server:AllowedOrigins:1"] = "https://admin.example.com",
    ["Server:Features"] = "search, export",
    ["Admin:Email"] = "  ops@example.com ",
    ["Admin:BackupEmail"] = " backup@example.com",
    ["Logging:Verbosity"] = "verbose",
};

var valid = ValueSource.FromPairs(validPairs);

// A misspelled key isn't an error by itself: Server:Port just falls back to its default.
var misspelled = ValueSource.FromPairs(validPairs.Append(new("Server:Prot", "8080")));

var invalid = ValueSource.FromPairs(new Dictionary<string, string?>
{
    ["Server:Port"] = "99999",
    ["Server:RequestTimeout"] = "30s",
    ["Server:MaxConnections"] = "0",
    ["Server:AllowedOrigins:0"] = "https://example.com",
    ["Server:AllowedOrigins:1"] = "not a uri",
    ["Admin:Email"] = "ops.example.com",
    ["Admin:BackupEmail"] = "backup.example.com",
    ["Logging:Verbosity"] = "Chatty",
});

Console.WriteLine("Valid configuration:");
var options = ServerOptions.From(contract.Read(valid));
Console.WriteLine($"  Host            {options.Host}");
Console.WriteLine($"  Port            {options.Port}");
Console.WriteLine($"  RequestTimeout  {options.RequestTimeout}");
Console.WriteLine($"  MaxConnections  {options.MaxConnections?.ToString() ?? "unlimited"}");
Console.WriteLine($"  AllowedOrigins  {string.Join(", ", options.AllowedOrigins)}");
Console.WriteLine($"  Features        {string.Join(", ", options.Features)}");
Console.WriteLine($"  AdminEmail      {options.AdminEmail}");
Console.WriteLine($"  BackupEmail     {options.BackupEmail ?? "(none)"}");
Console.WriteLine($"  ApiKey          {(options.ApiKey is null ? "(none)" : "(set)")}");
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

// Server and Admin belong to this application, so a key there that the contract doesn't define is a mistake.
// Logging is shared with Microsoft.Extensions.Logging, so it isn't checked. WarningsAsErrors makes the mistake fail the read.
Console.WriteLine();
Console.WriteLine("Misspelled key, read strictly:");
try
{
    contract.Read(misspelled, new ReadOptions { CheckedSections = ["Server", "Admin"], WarningsAsErrors = true });
}
catch (InvalidConfigurationException exception)
{
    Console.WriteLine(exception.Message);
}

// The contract describes itself. Documentation, the contract file and an example configuration are rendered
// from the same descriptor. The contract file can be committed, so changes to the contract show up in review.
var descriptor = contract.CreateDescriptor();
var documentationPath = Path.Combine(AppContext.BaseDirectory, "configuration.md");
var contractPath = Path.Combine(AppContext.BaseDirectory, "configuration.contract.json");
var examplePath = Path.Combine(AppContext.BaseDirectory, "appsettings.example.json");
File.WriteAllText(documentationPath, Documentation.WriteMarkdown(descriptor, "Server configuration"));
File.WriteAllText(contractPath, ContractSerializer.WriteJson(descriptor));
File.WriteAllText(examplePath, ConfigurationGenerator.WriteJson(descriptor));

Console.WriteLine();
Console.WriteLine("Documentation:");
Console.WriteLine($"  {documentationPath}");
Console.WriteLine($"  {contractPath}");
Console.WriteLine($"  {examplePath}");
