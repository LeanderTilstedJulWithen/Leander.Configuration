using Leander.Primitives;

namespace Leander.Configuration.MicrosoftExtensions.Sample;

// Every configuration key the server knows about, with its type, presence and description.
public static class ServerConfiguration
{
    // A JSON array becomes indexed keys: Server:AllowedOrigins:0, Server:AllowedOrigins:1, ...
    public static readonly ConfigurationDefinition<IReadOnlyList<Uri>> AllowedOrigins =
        ConfigurationDefinition.Indexed("Server:AllowedOrigins", SamplePrimitives.Origins)
            .Describe("Origins allowed to call the server.");

    // Optional: not in appsettings.json, so it's null unless given, e.g. on the command line.
    public static readonly ConfigurationDefinition<string?> CertificatePath =
        ConfigurationDefinition.Define("Server:CertificatePath", SamplePrimitives.FilePath)
            .Optional()
            .Describe("TLS certificate file. Plain HTTP when missing.");

    public static readonly ConfigurationDefinition<IReadOnlyList<string>> Features =
        ConfigurationDefinition.Define("Server:Features", SamplePrimitives.Features)
            .Default([])
            .Describe("Comma-separated list of enabled features.");

    public static readonly ConfigurationDefinition<string> Host =
        ConfigurationDefinition.Define("Server:Host", Primitive.String)
            .Default("localhost")
            .Describe("The host name to listen on.");

    public static readonly ConfigurationDefinition<int> Port =
        ConfigurationDefinition.Define("Server:Port", SamplePrimitives.Port)
            .Default(8080)
            .Describe("The port to listen on.");
}
