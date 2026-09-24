using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives.Tests;

public class PrimitiveDefinitionTests
{
    [Fact]
    public void Define_WithoutArguments_IsUnnamedWithoutConverter()
    {
        var definition = PrimitiveDefinition.Define<int>();

        Assert.Null(definition.Name);
        Assert.Null(definition.Description);
        Assert.Null(definition.Converter);
        Assert.Equal(typeof(int), definition.ValueType);
        Assert.Empty(definition.Normalizers);
        Assert.Empty(definition.Validators);
    }

    [Fact]
    public void Define_WithConverter_IsUnnamed()
    {
        var definition = PrimitiveDefinition.Define(Converters.Int32Hex);

        Assert.Null(definition.Name);
        Assert.Same(Converters.Int32Hex, definition.Converter);
    }

    [Fact]
    public void Define_WithName_HasNoConverter()
    {
        var definition = PrimitiveDefinition.Define<int>("Port");

        Assert.Equal("Port", definition.Name);
        Assert.Null(definition.Converter);
    }

    [Fact]
    public void Define_WithNameAndConverter()
    {
        var definition = PrimitiveDefinition.Define("Hex", Converters.Int32Hex);

        Assert.Equal("Hex", definition.Name);
        Assert.Same(Converters.Int32Hex, definition.Converter);
    }

    [Fact]
    public void Describe_ReturnsNewDefinitionAndLeavesOriginalUnchanged()
    {
        var original = PrimitiveDefinition.Define<int>("Port");

        var described = original.Describe("A TCP port.");

        Assert.NotSame(original, described);
        Assert.Null(original.Description);
        Assert.Equal("A TCP port.", described.Description);
        Assert.Equal("Port", described.Name);
    }

    [Fact]
    public void Describe_ReplacesPreviousDescription() =>
        Assert.Equal("second", PrimitiveDefinition.Define<int>().Describe("first").Describe("second").Description);

    [Fact]
    public void Normalize_AppendsInOrderAndLeavesOriginalUnchanged()
    {
        var first = Normalizers.Create<string>("first", value => value);
        var second = Normalizers.Create<string>("second", value => value);
        var original = PrimitiveDefinition.Define<string>().Normalize(first);

        var extended = original.Normalize(second);

        Assert.Equal([first], original.Normalizers);
        Assert.Equal([first, second], extended.Normalizers);
    }

    [Fact]
    public void Validate_AppendsInOrderAndLeavesOriginalUnchanged()
    {
        var first = Validators.GreaterThan(0);
        var second = Validators.LessThan(10);
        var original = PrimitiveDefinition.Define<int>().Validate(first);

        var extended = original.Validate(second);

        Assert.Equal([first], original.Validators);
        Assert.Equal([first, second], extended.Validators);
    }

    [Fact]
    public void BuilderMethods_PreserveOtherSettings()
    {
        var normalizer = Normalizers.Trim;
        var validator = Validators.NotEmpty;

        var definition = PrimitiveDefinition.Define("Name", Converters.String)
            .Describe("A name.")
            .Normalize(normalizer)
            .Validate(validator);

        Assert.Equal("Name", definition.Name);
        Assert.Equal("A name.", definition.Description);
        Assert.Same(Converters.String, definition.Converter);
        Assert.Equal([normalizer], definition.Normalizers);
        Assert.Equal([validator], definition.Validators);
    }
}
