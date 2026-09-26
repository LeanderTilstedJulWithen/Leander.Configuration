using Leander.Primitives;
using Leander.Primitives.Validation;

namespace Leander.Configuration.MicrosoftExtensions.Sample;

// Every configuration key the server knows about, with its type, presence and description.
public static class ServerConfiguration
{
    public static readonly PrimitiveDefinition<int> PortPrimitive =
        PrimitiveDefinition.Define<int>("Port")
            .Validate(Validators.InRange(1, 65535))
            .Describe("A TCP port.");

    public static readonly ConfigurationDefinition<string> Host =
        ConfigurationDefinition.Define<string>("Server:Host")
            .Default("localhost")
            .Describe("The host name to listen on.");

    public static readonly ConfigurationDefinition<int> Port =
        ConfigurationDefinition.Define("Server:Port", PortPrimitive)
            .Default(8080)
            .Describe("The port to listen on.");

    // A JSON array becomes indexed keys: Server:AllowedOrigins:0, Server:AllowedOrigins:1, ...
    public static readonly ConfigurationDefinition<IReadOnlyList<Uri>> AllowedOrigins =
        ConfigurationDefinition.Define<Uri>("Server:AllowedOrigins")
            .Indexed()
            .Validate(Validators.Collections.NotEmpty)
            .Describe("Origins allowed to call the server.");

    public static readonly ConfigurationDefinition<IReadOnlyList<string>> Features =
        ConfigurationDefinition.Define<string>("Server:Features")
            .Delimited()
            .Default([])
            .Describe("Comma-separated list of enabled features.");
}
