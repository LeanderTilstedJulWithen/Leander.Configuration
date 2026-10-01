using Leander.Primitives;
using Leander.Primitives.Parsing;

namespace Leander.Configuration.Tests;

public class ConfigurationContractBuilderTests
{
    private static ConfigurationContract Build(params ConfigurationDefinition[] definitions)
    {
        var builder = new ConfigurationContractBuilder();
        foreach (var definition in definitions)
        {
            builder.Register(definition);
        }

        return builder.Build();
    }

    private static string BuildFailure(params ConfigurationDefinition[] definitions) =>
        Assert.Throws<InvalidOperationException>(() => Build(definitions)).Message;

    [Fact]
    public void Build_KeepsDefinitionsInRegistrationOrder()
    {
        var port = ConfigurationDefinition.Define("Port", Primitive.Int32);
        var host = ConfigurationDefinition.Define("Host", Primitive.String);

        var contract = Build(port, host);

        Assert.Equal([port, host], contract.Definitions);
        Assert.True(contract.Contains(port));
        Assert.False(contract.Contains(ConfigurationDefinition.Define("Other", Primitive.String)));
    }

    [Fact]
    public void Build_Empty_IsValid()
    {
        Assert.Empty(Build().Definitions);
    }

    // Duplicate keys

    [Fact]
    public void Build_DuplicateKey_Fails()
    {
        var message = BuildFailure(
            ConfigurationDefinition.Define("Port", Primitive.Int32),
            ConfigurationDefinition.Define("Port", Primitive.String));

        Assert.Contains("Port: defined more than once.", message);
    }

    [Fact]
    public void Build_DuplicateKeyDifferingInCase_Fails()
    {
        var message = BuildFailure(
            ConfigurationDefinition.Define("Server:Port", Primitive.Int32),
            ConfigurationDefinition.Define("server:port", Primitive.Int32));

        Assert.Contains("defined more than once.", message);
    }

    // Primitive names

    [Fact]
    public void Build_SamePrimitiveInstanceOnSeveralKeys_IsValid()
    {
        var port = new Primitive<int>("Port", Converters.Int32);

        Build(
            ConfigurationDefinition.Define("Server:Port", port),
            ConfigurationDefinition.Define("Admin:Port", port));
    }

    [Fact]
    public void Build_TwoPrimitivesWithSameTypeAndName_Fails()
    {
        var message = BuildFailure(
            ConfigurationDefinition.Define("Server:Port", new Primitive<int>("Port", Converters.Int32)),
            ConfigurationDefinition.Define("Admin:Port", new Primitive<int>("Port", Converters.Int32)));

        Assert.Contains("Admin:Port: another primitive is named Int32 (Port).", message);
    }

    [Fact]
    public void Build_SameNameDifferentType_IsValid()
    {
        Build(
            ConfigurationDefinition.Define("Signed", Primitive.Int32Hex),
            ConfigurationDefinition.Define("Unsigned", Primitive.UInt32Hex));
    }

    [Fact]
    public void Build_NameClashWithABase_Fails()
    {
        var port = new Primitive<int>("Port", Converters.Int32);
        var adminPort = new Primitive<int>("AdminPort", new Primitive<int>("Port", Converters.Int32));

        var message = BuildFailure(
            ConfigurationDefinition.Define("Server:Port", port),
            ConfigurationDefinition.Define("Admin:Port", adminPort));

        Assert.Contains("Admin:Port: another primitive is named Int32 (Port).", message);
    }

    [Fact]
    public void Build_NameClashWithAListElement_Fails()
    {
        var port = new Primitive<int>("Port", Converters.Int32);
        var ports = new ListPrimitive<int>("Ports", new Primitive<int>("Port", Converters.Int32));

        var message = BuildFailure(
            ConfigurationDefinition.Define("Server:Port", port),
            ConfigurationDefinition.Indexed("Server:Ports", ports));

        Assert.Contains("Server:Ports: another primitive is named Int32 (Port).", message);
    }

    [Fact]
    public void Build_NameClash_IsReportedOnce()
    {
        var message = BuildFailure(
            ConfigurationDefinition.Define("A", new Primitive<int>("Port", Converters.Int32)),
            ConfigurationDefinition.Define("B", new Primitive<int>("Port", Converters.Int32)),
            ConfigurationDefinition.Define("C", new Primitive<int>("Port", Converters.Int32)));

        Assert.Single(message.Split(Environment.NewLine), line => line.Contains("another primitive"));
    }

    // Presence

    [Fact]
    public void Build_NullDefault_Fails()
    {
        var message = BuildFailure(ConfigurationDefinition.Define("Name", Primitive.String).Default(null!));

        Assert.Contains("Name: the default is null, but the definition is not optional.", message);
    }

    [Fact]
    public void Build_DefaultAfterOptional_Fails()
    {
        var message = BuildFailure(ConfigurationDefinition.Define("Port", Primitive.Int32).Optional().Default(80));

        Assert.Contains("Port: Optional() cannot be combined with a default.", message);
    }

    [Fact]
    public void Build_DefaultBeforeOptional_Fails()
    {
        var message = BuildFailure(
            ConfigurationDefinition.Define("Port", Primitive.Int32).Default(80).Optional(),
            ConfigurationDefinition.Define("Name", Primitive.String).Default("x").Optional());

        Assert.Contains("Port: Optional() cannot be combined with a default.", message);
        Assert.Contains("Name: Optional() cannot be combined with a default.", message);
    }

    [Fact]
    public void Build_ReportsEveryFailureAtOnce()
    {
        var message = BuildFailure(
            ConfigurationDefinition.Define("Port", Primitive.Int32),
            ConfigurationDefinition.Define("Port", Primitive.Int32),
            ConfigurationDefinition.Define("A", new Primitive<int>("Count", Converters.Int32)),
            ConfigurationDefinition.Define("B", new Primitive<int>("Count", Converters.Int32)),
            ConfigurationDefinition.Define("Name", Primitive.String).Default(null!));

        Assert.StartsWith("Configuration contract could not be built:", message);
        Assert.Contains("Port: defined more than once.", message);
        Assert.Contains("B: another primitive is named Int32 (Count).", message);
        Assert.Contains("Name: the default is null", message);
    }
}
