using Leander.Primitives;

namespace Leander.Configuration.Generators.Sample;

// On the type: every static definition in it is registered by RegisterFrom, whatever its accessibility.
[RegisterConfiguration]
public static class ServerConfiguration
{
    public static readonly ConfigurationDefinition<IReadOnlyList<Uri>> AllowedOrigins =
        ConfigurationDefinition.Indexed("Server:AllowedOrigins", SamplePrimitives.Origins)
            .Describe("Origins allowed to call the server.");

    public static readonly ConfigurationDefinition<string> Host =
        ConfigurationDefinition.Define("Server:Host", Primitive.String)
            .Default("localhost")
            .Describe("The host name to listen on.");

    public static readonly ConfigurationDefinition<int> Port =
        ConfigurationDefinition.Define("Server:Port", SamplePrimitives.Port)
            .Default(8080)
            .Describe("The port to listen on.");

    internal static readonly ConfigurationDefinition<string?> CertificatePath =
        ConfigurationDefinition.Define("Server:CertificatePath", Primitive.String)
            .Optional()
            .Describe("TLS certificate file. Plain HTTP when missing.");
}
