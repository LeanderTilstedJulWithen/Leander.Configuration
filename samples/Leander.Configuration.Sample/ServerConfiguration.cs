using Leander.Primitives.Validation;

namespace Leander.Configuration.Sample;

public enum Verbosity
{
    Quiet,
    Normal,
    Verbose,
}

// Every configuration key the server knows about, with its type, presence and description.
public static class ServerConfiguration
{
    public static readonly ConfigurationDefinition<string> Host =
        ConfigurationDefinition.Define<string>("Server:Host")
            .Default("localhost")
            .Describe("The host name to listen on.");

    public static readonly ConfigurationDefinition<int> Port =
        ConfigurationDefinition.Define("Server:Port", SamplePrimitives.Port)
            .Default(8080)
            .Describe("The port to listen on.");

    public static readonly ConfigurationDefinition<TimeSpan> RequestTimeout =
        ConfigurationDefinition.Define<TimeSpan>("Server:RequestTimeout")
            .Default(TimeSpan.FromSeconds(30))
            .Describe("Maximum time allowed for a request.");

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

    public static readonly ConfigurationDefinition<string> AdminEmail =
        ConfigurationDefinition.Define("Admin:Email", SamplePrimitives.Email)
            .Describe("Where operational alerts are sent.");

    public static readonly ConfigurationDefinition<Verbosity> Verbosity =
        ConfigurationDefinition.Define<Verbosity>("Logging:Verbosity")
            .Default(Sample.Verbosity.Normal)
            .Describe("How much the server logs.");
}
