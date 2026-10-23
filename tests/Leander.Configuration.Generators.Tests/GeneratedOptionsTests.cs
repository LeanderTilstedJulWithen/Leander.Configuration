using Leander.Primitives;

namespace Leander.Configuration.Generators.Tests;

// The generator runs on this project: these options types are built by generated code from a real snapshot.
public partial class GeneratedOptionsTests
{
    private static readonly ConfigurationSnapshot Snapshot = new ConfigurationContractBuilder()
        .RegisterFrom(typeof(Definitions))
        .Build()
        .Read(ValueSource.FromPairs(new Dictionary<string, string?>
        {
            ["Server:Host"] = "example.com",
            ["Server:Origins:0"] = "https://example.com",
            ["Server:Origins:1"] = "https://admin.example.com",
            ["Api:Key"] = "secret",
        }));

    [Fact]
    public void Properties_AreSetFromTheSnapshot()
    {
        var options = PropertyOptions.FromSnapshot(Snapshot);

        Assert.Equal("example.com", options.Host);
        Assert.Equal(8080, options.Port);
        Assert.Null(options.Timeout);
        Assert.Equal([new Uri("https://example.com"), new Uri("https://admin.example.com")], options.Origins);
        Assert.Equal("unset", options.NotFromConfiguration);
    }

    [Fact]
    public void RecordParameters_AreSetFromTheSnapshot()
    {
        var options = RecordOptions.FromSnapshot(Snapshot);

        Assert.Equal(new RecordOptions("example.com", 8080, null), options);
    }

    [Fact]
    public void ConstructorParametersAndProperties_AreBothSet()
    {
        var options = Factories.Mixed(Snapshot);

        Assert.Equal("example.com", options.Host);
        Assert.Equal(8080, options.Port);
    }

    [Fact]
    public void Struct_IsSetFromTheSnapshot()
    {
        var options = StructOptions.FromSnapshot(Snapshot);

        Assert.Equal(8080, options.Port);
    }

    [Fact]
    public void PrivateDefinitionsOnTheOptionsType_AreUsed()
    {
        var options = SelfContainedOptions.FromSnapshot(new ConfigurationContractBuilder()
            .RegisterFrom(typeof(SelfContainedOptions))
            .Build()
            .Read(ValueSource.FromPairs(new Dictionary<string, string?> { ["Api:Key"] = "secret" })));

        Assert.Equal("secret", options.Key);
    }

    [Fact]
    public void DefinitionFromAnotherType_IsUsed()
    {
        var options = Factories.WithKey(Snapshot);

        Assert.Equal("example.com", options.Host);
        Assert.Equal("secret", options.Key);
    }

    [RegisterConfiguration]
    public static class Definitions
    {
        public static readonly ConfigurationDefinition<string> Host = ConfigurationDefinition.Define("Server:Host", Primitive.String);

        public static readonly ConfigurationDefinition<IReadOnlyList<Uri>> Origins =
            ConfigurationDefinition.Indexed("Server:Origins", new ListPrimitive<Uri>("Origins", Primitive.Uri));

        public static readonly ConfigurationDefinition<int> Port = ConfigurationDefinition.Define("Server:Port", Primitive.Int32).Default(8080);

        public static readonly ConfigurationDefinition<TimeSpan?> Timeout = ConfigurationDefinition.Define("Server:Timeout", Primitive.TimeSpan).Optional();

        internal static ConfigurationDefinition<string> Key { get; } = ConfigurationDefinition.Define("Api:Key", Primitive.String).Sensitive();
    }

    public sealed partial class PropertyOptions
    {
        [FromDefinition(nameof(Definitions.Host))]
        public required string Host { get; init; }

        public string NotFromConfiguration { get; set; } = "unset";

        [FromDefinition(nameof(Definitions.Origins))]
        public IReadOnlyList<Uri> Origins { get; set; } = [];

        [FromDefinition(nameof(Definitions.Port))]
        public int Port { get; init; }

        [FromDefinition(nameof(Definitions.Timeout))]
        public TimeSpan? Timeout { get; init; }

        [GeneratedOptions(typeof(Definitions))]
        public static partial PropertyOptions FromSnapshot(ConfigurationSnapshot snapshot);
    }

    public sealed partial record RecordOptions(
        [FromDefinition(nameof(Definitions.Host))] string Host,
        [FromDefinition(nameof(Definitions.Port))] int Port,
        [FromDefinition(nameof(Definitions.Timeout))] TimeSpan? Timeout)
    {
        [GeneratedOptions(typeof(Definitions))]
        internal static partial RecordOptions FromSnapshot(ConfigurationSnapshot snapshot);
    }

    public partial struct StructOptions
    {
        [FromDefinition(typeof(Definitions), nameof(Definitions.Port))]
        public int Port { get; set; }

        [GeneratedOptions]
        public static partial StructOptions FromSnapshot(ConfigurationSnapshot snapshot);
    }

    public sealed partial class SelfContainedOptions
    {
        [RegisterConfiguration]
        private static readonly ConfigurationDefinition<string> KeyDefinition = ConfigurationDefinition.Define("Api:Key", Primitive.String);

        [FromDefinition(nameof(KeyDefinition))]
        public required string Key { get; init; }

        [GeneratedOptions]
        public static partial SelfContainedOptions FromSnapshot(ConfigurationSnapshot snapshot);
    }

    public sealed class MixedOptions([FromDefinition(typeof(Definitions), nameof(Definitions.Host))] string host)
    {
        public string Host { get; } = host;

        [FromDefinition(typeof(Definitions), nameof(Definitions.Port))]
        public int Port { get; init; }
    }

    public sealed class KeyOptions
    {
        [FromDefinition(nameof(Definitions.Host))]
        public required string Host { get; init; }

        [FromDefinition(typeof(Definitions), nameof(Definitions.Key))]
        public required string Key { get; init; }
    }

    // The method doesn't have to be on the options type.
    public static partial class Factories
    {
        [GeneratedOptions]
        public static partial MixedOptions Mixed(ConfigurationSnapshot snapshot);

        [GeneratedOptions(typeof(Definitions))]
        public static partial KeyOptions WithKey(ConfigurationSnapshot snapshot);
    }
}
