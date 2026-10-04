using Leander.Primitives;

namespace Leander.Configuration.Tooling.Sample;

public enum Verbosity
{
    Quiet,
    Normal,
    Verbose,
}

// Every configuration key the server knows about. Between them they use everything the documentation can show.
public static class ServerConfiguration
{
    public static readonly ConfigurationDefinition<string> Host =
        ConfigurationDefinition.Define("Server:Host", Primitive.String)
            .Default("localhost")
            .Describe("The host name to listen on.");

    public static readonly ConfigurationDefinition<int> Port =
        ConfigurationDefinition.Define("Server:Port", SamplePrimitives.Port)
            .Default(8080)
            .Describe("The port to listen on.");

    public static readonly ConfigurationDefinition<TimeSpan> RequestTimeout =
        ConfigurationDefinition.Define("Server:RequestTimeout", Primitive.TimeSpan)
            .Default(TimeSpan.FromSeconds(30))
            .Describe("Maximum time allowed for a request.");

    public static readonly ConfigurationDefinition<int?> MaxConnections =
        ConfigurationDefinition.Define("Server:MaxConnections", SamplePrimitives.ConnectionLimit)
            .Optional()
            .Describe("Maximum number of concurrent connections. No limit when missing.");

    // An entry per item: Server:AllowedOrigins:0, Server:AllowedOrigins:1, ...
    public static readonly ConfigurationDefinition<IReadOnlyList<Uri>> AllowedOrigins =
        ConfigurationDefinition.Indexed("Server:AllowedOrigins", SamplePrimitives.Origins)
            .Describe("Origins allowed to call the server.");

    // One entry holding a delimited list.
    public static readonly ConfigurationDefinition<IReadOnlyList<string>> Features =
        ConfigurationDefinition.Define("Server:Features", SamplePrimitives.Features)
            .Default([])
            .Describe("Enabled features.");

    // Sensitive: the documentation and the example configuration never show the value.
    public static readonly ConfigurationDefinition<string?> ApiKey =
        ConfigurationDefinition.Define("Server:ApiKey", Primitive.String)
            .Sensitive()
            .Optional()
            .Describe("Key for calling the payment provider. Payments are disabled when missing.");

    public static readonly ConfigurationDefinition<int> AdminPort =
        ConfigurationDefinition.Define("Admin:Port", SamplePrimitives.UnprivilegedPort)
            .Default(9090)
            .Describe("The port of the admin interface.");

    public static readonly ConfigurationDefinition<string> AdminEmail =
        ConfigurationDefinition.Define("Admin:Email", SamplePrimitives.Email)
            .Describe("Where operational alerts are sent.");

    public static readonly ConfigurationDefinition<Verbosity> Verbosity =
        ConfigurationDefinition.Define("Logging:Verbosity", Primitive.Enum<Verbosity>())
            .Default(Sample.Verbosity.Normal)
            .Describe("How much the server logs.");

    public static readonly ConfigurationDefinition<int> TraceMask =
        ConfigurationDefinition.Define("Logging:TraceMask", SamplePrimitives.Mask)
            .Default(0x0F)
            .Describe("Which subsystems write trace messages, one bit each.");
}
