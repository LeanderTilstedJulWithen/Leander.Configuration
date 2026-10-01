using Leander.Configuration.Descriptors;
using Leander.Primitives;
using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Configuration.Tests;

public class ContractDescriptorTests
{
    private enum Color
    {
        Red,
        Green,
    }

    private static readonly Primitive<int> Port = new("Port", Converters.Int32)
    {
        Validators = [Validators.InRange(1, 65535)],
        Description = "A TCP port.",
    };

    private static ContractDescriptor Describe(params ConfigurationDefinition[] definitions)
    {
        var builder = new ConfigurationContractBuilder();
        foreach (var definition in definitions)
        {
            builder.Register(definition);
        }

        return builder.Build().CreateDescriptor();
    }

    private static PrimitiveDescriptor Described(ContractDescriptor contract, string name) =>
        Assert.Single(contract.Primitives, primitive => primitive.Name == name);

    [Fact]
    public void Definition_HasKeyDescriptionAndValue()
    {
        var contract = Describe(ConfigurationDefinition.Define("Server:Port", Port).Default(8080).Describe("The port."));

        var definition = Assert.Single(contract.Definitions);
        Assert.Equal("Server:Port", definition.Key);
        Assert.Equal("The port.", definition.Description);
        Assert.False(definition.IsSensitive);
        Assert.Equal("Int32", definition.Value.Type);
        Assert.Equal(ValuePresence.Default, definition.Value.Presence);
        Assert.Equal(ValueForm.Scalar, definition.Value.Form);
        Assert.Equal("8080", definition.Value.Default);
        Assert.Null(definition.Value.DefaultItems);
        Assert.Equal(new PrimitiveReference("Int32", "Port"), definition.Value.Primitive);
    }

    [Fact]
    public void Definitions_AreInRegistrationOrder()
    {
        var contract = Describe(
            ConfigurationDefinition.Define("B", Primitive.String),
            ConfigurationDefinition.Define("A", Primitive.String));

        Assert.Equal(["B", "A"], contract.Definitions.Select(definition => definition.Key));
    }

    [Fact]
    public void Presence_RequiredAndOptional()
    {
        var contract = Describe(
            ConfigurationDefinition.Define("Required", Primitive.Int32),
            ConfigurationDefinition.Define("Optional", Primitive.Int32).Optional());

        Assert.Equal(ValuePresence.Required, contract.Definitions[0].Value.Presence);
        Assert.Equal(ValuePresence.Optional, contract.Definitions[1].Value.Presence);
        Assert.Equal("Int32", contract.Definitions[1].Value.Type);
        Assert.Null(contract.Definitions[1].Value.Default);
    }

    [Fact]
    public void Default_IsFormattedByTheConverter()
    {
        var contract = Describe(ConfigurationDefinition.Define("Timeout", Primitive.TimeSpan).Default(TimeSpan.FromSeconds(30)));

        Assert.Equal("00:00:30", contract.Definitions[0].Value.Default);
    }

    [Fact]
    public void Sensitive_LeavesDefaultOut()
    {
        var contract = Describe(
            ConfigurationDefinition.Define("Key", Primitive.String).Default("secret").Sensitive(),
            ConfigurationDefinition.Indexed("Keys", new ListPrimitive<string>("Keys", Primitive.String)).Default(["secret"]).Sensitive());

        Assert.All(contract.Definitions, definition =>
        {
            Assert.True(definition.IsSensitive);
            Assert.Equal(ValuePresence.Default, definition.Value.Presence);
            Assert.Null(definition.Value.Default);
            Assert.Null(definition.Value.DefaultItems);
        });
    }

    [Fact]
    public void Indexed_HasFormListPrimitiveAndDefaultItems()
    {
        var ports = new ListPrimitive<int>("Ports", Port, ';');
        var contract = Describe(ConfigurationDefinition.Indexed("Ports", ports).Default([80, 443]));

        var value = contract.Definitions[0].Value;
        Assert.Equal(ValueForm.Indexed, value.Form);
        Assert.Equal("IReadOnlyList<Int32>", value.Type);
        Assert.Equal(new PrimitiveReference("IReadOnlyList<Int32>", "Ports"), value.Primitive);
        Assert.Equal("80;443", value.Default);
        Assert.Equal(["80", "443"], value.DefaultItems);
    }

    [Fact]
    public void Indexed_DefaultItems_KeepItemsContainingTheDelimiter()
    {
        var rows = new ListPrimitive<IReadOnlyList<int>>("Rows", new ListPrimitive<int>("Row", Primitive.Int32));
        var contract = Describe(ConfigurationDefinition.Indexed("Rows", rows).Default([[1, 2], [3]]));

        Assert.Equal(["1,2", "3"], contract.Definitions[0].Value.DefaultItems);
    }

    [Fact]
    public void DelimitedList_IsScalarWithoutDefaultItems()
    {
        var contract = Describe(ConfigurationDefinition.Define("Hosts", new ListPrimitive<string>("Hosts", Primitive.String)).Default(["a", "b"]));

        Assert.Equal(ValueForm.Scalar, contract.Definitions[0].Value.Form);
        Assert.Equal("a,b", contract.Definitions[0].Value.Default);
        Assert.Null(contract.Definitions[0].Value.DefaultItems);
    }

    [Fact]
    public void Primitive_HasItsOwnRulesAndDescription()
    {
        var name = new Primitive<string>("Name", Converters.String)
        {
            Normalizers = [Normalizers.Trim],
            Validators = [Validators.NotEmpty],
            Description = "A name.",
        };

        var primitive = Described(Describe(ConfigurationDefinition.Define("Name", name)), "Name");

        Assert.Equal("String", primitive.Type);
        Assert.Equal("A name.", primitive.Description);
        Assert.Equal(["trim whitespace"], primitive.Normalizers);
        Assert.Equal(["must not be empty"], primitive.Validators);
        Assert.Null(primitive.Base);
        Assert.Null(primitive.Element);
        Assert.Null(primitive.Delimiter);
        Assert.Null(primitive.Values);
    }

    [Fact]
    public void Primitive_RuleArgumentsAreFormattedWithTheConverter()
    {
        var timeout = new Primitive<TimeSpan>("Timeout", Converters.TimeSpan)
        {
            Normalizers = [Normalizers.UpperBound(TimeSpan.FromMinutes(5))],
            Validators = [Validators.GreaterThan(TimeSpan.Zero)],
        };

        var primitive = Described(Describe(ConfigurationDefinition.Define("Timeout", timeout)), "Timeout");

        Assert.Equal(["upper bound 00:05:00"], primitive.Normalizers);
        Assert.Equal(["must be greater than 00:00:00"], primitive.Validators);
    }

    [Fact]
    public void Primitive_Derived_RefersToBaseWhichIsDescribedToo()
    {
        var adminPort = new Primitive<int>("AdminPort", Port) { Validators = [Validators.GreaterThan(1024)] };

        var contract = Describe(ConfigurationDefinition.Define("Admin:Port", adminPort));

        Assert.Equal(["AdminPort", "Port"], contract.Primitives.Select(primitive => primitive.Name));
        Assert.Equal(new PrimitiveReference("Int32", "Port"), contract.Primitives[0].Base);
        Assert.Equal(["must be greater than 1024"], contract.Primitives[0].Validators);
        Assert.Equal(["must be between 1 and 65535"], contract.Primitives[1].Validators);
    }

    [Fact]
    public void Primitive_List_HasElementAndDelimiter()
    {
        var contract = Describe(ConfigurationDefinition.Define("Ports", new ListPrimitive<int>("Ports", Port, ';')));

        Assert.Equal(["Ports", "Port"], contract.Primitives.Select(primitive => primitive.Name));
        Assert.Equal(new PrimitiveReference("Int32", "Port"), contract.Primitives[0].Element);
        Assert.Equal(';', contract.Primitives[0].Delimiter);
    }

    [Fact]
    public void Primitive_Enum_HasValues()
    {
        var primitive = Described(Describe(ConfigurationDefinition.Define("Color", Primitive.Enum<Color>())), "Color");

        Assert.Equal(["Red", "Green"], primitive.Values);
    }

    [Fact]
    public void Primitives_AreDescribedOnceInOrderOfFirstUse()
    {
        var contract = Describe(
            ConfigurationDefinition.Define("A", Primitive.String),
            ConfigurationDefinition.Define("B", Port),
            ConfigurationDefinition.Define("C", Primitive.String));

        Assert.Equal(["String", "Port"], contract.Primitives.Select(primitive => primitive.Name));
    }
}
