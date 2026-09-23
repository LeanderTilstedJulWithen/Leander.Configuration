using static Leander.Configuration.Tests.TestHelpers;

namespace Leander.Configuration.Tests;

public class ScalarDefinitionTests
{
    [Fact]
    public void Get_ValuePresent_ReturnsParsedValue()
    {
        var definition = ConfigurationDefinition.Define<int>("Database:Timeout");
        var reader = Reader(("Database:Timeout", "42"));

        Assert.Equal(42, reader.Get(definition));
        Assert.False(reader.HasErrors);
    }

    [Fact]
    public void Get_KeyLookup_IsCaseInsensitive()
    {
        var definition = ConfigurationDefinition.Define<int>("Database:Timeout");
        var reader = Reader(("database:TIMEOUT", "42"));

        Assert.Equal(42, reader.Get(definition));
    }

    [Fact]
    public void Get_MissingRequired_ReportsError()
    {
        var definition = ConfigurationDefinition.Define<string>("Database:ConnectionString").Required();
        var reader = Reader();

        Assert.False(reader.TryGet(definition, out _));

        var error = SingleError(reader);
        Assert.Equal("Database:ConnectionString", error.Key);
        Assert.Equal("value is required", error.Message);
        Assert.Same(definition, error.Definition);
    }

    [Fact]
    public void Get_MissingWithDefault_ReturnsDefault()
    {
        var definition = ConfigurationDefinition.Define<int>("Database:Timeout").Default(30);
        var reader = Reader();

        Assert.Equal(30, reader.Get(definition));
        Assert.Empty(reader.Diagnostics);
    }

    [Fact]
    public void Get_MissingOptional_ReturnsDefaultOfTAndReportsTrace()
    {
        var definition = ConfigurationDefinition.Define<int>("Database:Timeout");
        var reader = Reader();

        Assert.True(reader.TryGet(definition, out var value));
        Assert.Equal(0, value);
        Assert.Equal(DiagnosticSeverity.Trace, Assert.Single(reader.Diagnostics).Severity);
    }

    [Fact]
    public void Default_ThenRequired_LastCallWins()
    {
        var definition = ConfigurationDefinition.Define<int>("Database:Timeout").Default(30).Required();

        Assert.True(definition.IsRequired);
        Assert.False(definition.HasDefault);
    }

    [Fact]
    public void Get_InvalidValue_ReportsParseError()
    {
        var definition = ConfigurationDefinition.Define<int>("Database:Timeout");
        var reader = Reader(("Database:Timeout", "abc"));

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("'abc' is not a valid Int32", SingleError(reader).Message);
    }

    [Fact]
    public void Get_EmptyString_IsPassedToParser()
    {
        var number = ConfigurationDefinition.Define<int>("Number").Required();
        var text = ConfigurationDefinition.Define<string>("Text").Required();
        var reader = Reader(("Number", ""), ("Text", ""));

        Assert.False(reader.TryGet(number, out _));
        Assert.Equal("", reader.Get(text));
        Assert.Equal("'' is not a valid Int32", SingleError(reader).Message);
    }

    [Fact]
    public void Get_ExplicitConverter_IsUsed()
    {
        var definition = ConfigurationDefinition.Define("Flags", Converters.Int32Hex);
        var reader = Reader(("Flags", "0x1F"));

        Assert.Equal(31, reader.Get(definition));
    }

    [Fact]
    public void Get_KeyedConverter_IsResolvedFromRegistry()
    {
        var definition = ConfigurationDefinition.Define<int>("Flags", "Hex");
        var reader = Reader(("Flags", "0x1F"));

        Assert.Equal(31, reader.Get(definition));
    }

    [Fact]
    public void Get_KeyedConverterParseFailure_MentionsConverterKey()
    {
        var definition = ConfigurationDefinition.Define<int>("Flags", "Hex");
        var reader = Reader(("Flags", "xyz"));

        reader.Get(definition);

        Assert.Equal("'xyz' is not a valid Int32 (Hex)", SingleError(reader).Message);
    }

    [Fact]
    public void Get_UnknownConverterKey_ReportsError()
    {
        var definition = ConfigurationDefinition.Define<int>("Flags", "Octal");
        var reader = Reader(("Flags", "17"));

        Assert.False(reader.TryGet(definition, out _));
        Assert.Equal("no converter is registered for Int32 with key 'Octal'", SingleError(reader).Message);
    }

    [Fact]
    public void Get_EnumWithoutExplicitConverter_UsesRegistryFallback()
    {
        var definition = ConfigurationDefinition.Define<DayOfWeek>("Day");
        var reader = Reader(("Day", "Friday"));

        Assert.Equal(DayOfWeek.Friday, reader.Get(definition));
    }

    [Fact]
    public void Get_ChildEntriesInsteadOfValue_ReportsWarning()
    {
        var definition = ConfigurationDefinition.Define<string>("Name");
        var reader = Reader(("Name:0", "a"));

        reader.Get(definition);

        Assert.Contains(reader.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Metadata_IsExposedThroughNonGenericBase()
    {
        ConfigurationDefinition definition = ConfigurationDefinition.Define<int>("Database:Timeout")
            .Default(30)
            .Describe("Command timeout in seconds.");

        Assert.Equal("Database:Timeout", definition.Key);
        Assert.Equal(typeof(int), definition.ValueType);
        Assert.Equal("Command timeout in seconds.", definition.Description);
        Assert.True(definition.HasDefault);
        Assert.False(definition.IsRequired);
    }

    [Fact]
    public void BuilderMethods_ReturnNewDefinitions()
    {
        var original = ConfigurationDefinition.Define<int>("Database:Timeout");
        var required = original.Required();

        Assert.NotSame(original, required);
        Assert.False(original.IsRequired);
    }
}
