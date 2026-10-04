using Microsoft.Extensions.Configuration;

namespace Leander.Configuration.MicrosoftExtensions.Sample;

// Any IConfiguration is a value source: the host's, one built by hand, or a section of either. Reading it needs no
// container. An invalid configuration throws InvalidConfigurationException, with every problem as a diagnostic.
public static class ReadingIConfiguration
{
    public static void Run()
    {
        Console.WriteLine("Reading IConfiguration directly:");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Server:Port"] = "99999",
                ["Server:AllowedOrigins:0"] = "not a uri",
            })
            .Build();

        try
        {
            ServerContract.Contract.Read(configuration.AsValueSource(), ServerContract.ReadOptions);
        }
        catch (InvalidConfigurationException exception)
        {
            foreach (var diagnostic in exception.Diagnostics)
            {
                Console.WriteLine($"  {diagnostic}");
            }
        }

        Console.WriteLine();
    }
}
