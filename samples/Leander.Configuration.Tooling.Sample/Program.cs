using System.Runtime.CompilerServices;
using Leander.Configuration;
using Leander.Configuration.Tooling;
using Leander.Configuration.Tooling.Sample;

var contract = new ConfigurationContractBuilder()
    .Register(ServerConfiguration.Host)
    .Register(ServerConfiguration.Port)
    .Register(ServerConfiguration.RequestTimeout)
    .Register(ServerConfiguration.MaxConnections)
    .Register(ServerConfiguration.AllowedOrigins)
    .Register(ServerConfiguration.Features)
    .Register(ServerConfiguration.ApiKey)
    .Register(ServerConfiguration.AdminPort)
    .Register(ServerConfiguration.AdminEmail)
    .Register(ServerConfiguration.Verbosity)
    .Register(ServerConfiguration.TraceMask)
    .Build();

// The contract describes itself. Every output is rendered from the same descriptor.
var descriptor = contract.CreateDescriptor();
var output = OutputDirectory();
var contractPath = Path.Combine(output, "configuration.contract.json");

// The contract file is committed, so comparing it with the current contract shows what changed since.
// Change a rule in SamplePrimitives and run again to see it here, before the file is rewritten.
Console.WriteLine("Changes since the committed contract file:");
if (File.Exists(contractPath))
{
    var committed = ContractSerializer.ReadJson(File.ReadAllText(contractPath));
    var differences = ContractDiff.Compare(committed, descriptor);
    foreach (var difference in differences)
    {
        Console.WriteLine($"  {difference}");
    }

    if (differences.Count == 0)
    {
        Console.WriteLine("  none");
    }
}
else
{
    Console.WriteLine("  no contract file yet");
}

Directory.CreateDirectory(output);
File.WriteAllText(Path.Combine(output, "configuration.md"), Documentation.WriteMarkdown(descriptor, "Server configuration"));
File.WriteAllText(contractPath, ContractSerializer.WriteJson(descriptor));
File.WriteAllText(Path.Combine(output, "appsettings.example.json"), ConfigurationGenerator.WriteJson(descriptor));

Console.WriteLine();
Console.WriteLine($"Written to {output}");

// Next to this file, wherever the sample is run from, so the committed output is always the one rewritten.
static string OutputDirectory([CallerFilePath] string source = "") => Path.Combine(Path.GetDirectoryName(source)!, "output");
