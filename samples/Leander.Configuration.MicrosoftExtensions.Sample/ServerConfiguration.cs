using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.MicrosoftExtensions.Sample;

// Every configuration key the server knows about, with its type, presence and description.
public static class ServerConfiguration
{
    public static readonly Primitive<int> PortPrimitive = new("Port", Converters.Int32)
    {
        Validators = [Validators.InRange(1, 65535)],
        Description = "A TCP port.",
    };

    public static readonly Primitive<string> FilePathPrimitive = new("FilePath", Primitive.String)
    {
        Normalizers = [Normalizers.FullPath],
        Description = "A file path, made absolute.",
    };

    public static readonly ListPrimitive<Uri> OriginsPrimitive = new("Origins", Primitive.Uri)
    {
        Validators = [Validators.Collections.NotEmpty],
        Description = "Origins, at least one.",
    };

    public static readonly ListPrimitive<string> FeaturesPrimitive = new("Features", Primitive.String)
    {
        Description = "Comma-separated feature names.",
    };

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
        ConfigurationDefinition.Indexed("Server:AllowedOrigins", OriginsPrimitive)
            .Describe("Origins allowed to call the server.");

    public static readonly ConfigurationDefinition<IReadOnlyList<string>> Features =
        ConfigurationDefinition.Define("Server:Features", FeaturesPrimitive)
            .Default([])
            .Describe("Comma-separated list of enabled features.");

    // Optional: not in appsettings.json, so it's null unless given, e.g. on the command line.
    public static readonly ConfigurationDefinition<string?> CertificatePath =
        ConfigurationDefinition.Define("Server:CertificatePath", FilePathPrimitive)
            .Optional()
            .Describe("TLS certificate file. Plain HTTP when missing.");
}
