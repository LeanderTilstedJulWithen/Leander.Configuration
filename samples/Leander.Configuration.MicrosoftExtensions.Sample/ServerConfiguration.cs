using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.MicrosoftExtensions.Sample;

// Every configuration key the server knows about, with its type, presence and description.
public static class ServerConfiguration
{
    public static readonly Primitive<int> PortPrimitive =
        Primitive.Create("Port", Converters.Int32)
            .Validate(Validators.InRange(1, 65535))
            .Describe("A TCP port.");

    public static readonly ConfigurationDefinition<string> Host =
        ConfigurationDefinition.Define("Server:Host", Primitive.String)
            .Default("localhost")
            .Describe("The host name to listen on.");

    public static readonly ConfigurationDefinition<int> Port =
        ConfigurationDefinition.Define("Server:Port", PortPrimitive)
            .Default(8080)
            .Describe("The port to listen on.");

    // A JSON array becomes indexed keys: Server:AllowedOrigins:0, Server:AllowedOrigins:1, ...
    public static readonly ConfigurationDefinition<IReadOnlyList<Uri>> AllowedOrigins =
        ConfigurationDefinition.Define("Server:AllowedOrigins", Primitive.Uri)
            .Indexed()
            .Validate(Validators.Collections.NotEmpty)
            .Describe("Origins allowed to call the server.");

    public static readonly ConfigurationDefinition<IReadOnlyList<string>> Features =
        ConfigurationDefinition.Define("Server:Features", Primitive.String)
            .Delimited()
            .Default([])
            .Describe("Comma-separated list of enabled features.");

    // Optional: not in appsettings.json, so it's null unless given, e.g. on the command line.
    public static readonly ConfigurationDefinition<string?> CertificatePath =
        ConfigurationDefinition.Define("Server:CertificatePath", Primitive.String.Normalize(Normalizers.FullPath))
            .Optional()
            .Describe("TLS certificate file. Plain HTTP when missing.");
}
