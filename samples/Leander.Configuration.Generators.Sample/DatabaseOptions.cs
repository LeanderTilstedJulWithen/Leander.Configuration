using Leander.Primitives;

namespace Leander.Configuration.Generators.Sample;

// Definitions don't have to be public static fields of a static class. Here they live on the options type itself:
// private, one of them a property, each marked on its own.
public sealed partial class DatabaseOptions
{
    [RegisterConfiguration]
    private static readonly ConfigurationDefinition<string> ConnectionStringDefinition =
        ConfigurationDefinition.Define("Database:ConnectionString", Primitive.String)
            .Sensitive()
            .Describe("Connection string of the application database.");

    [RegisterConfiguration]
    private static ConfigurationDefinition<int> CommandTimeoutDefinition { get; } =
        ConfigurationDefinition.Define("Database:CommandTimeout", SamplePrimitives.Seconds)
            .Default(30)
            .Describe("Maximum time in seconds allowed for a database command.");

    [FromDefinition(nameof(CommandTimeoutDefinition))]
    public required int CommandTimeout { get; init; }

    [FromDefinition(nameof(ConnectionStringDefinition))]
    public required string ConnectionString { get; init; }

    // Without a type, definitions are looked up in the type that declares the method, so private ones work.
    [GeneratedOptions]
    public static partial DatabaseOptions FromSnapshot(ConfigurationSnapshot snapshot);
}
