using Leander.Primitives;

namespace Leander.Configuration.Tests;

public class ConfigurationDefinitionTests
{
    private static readonly Primitive<int> Port = new("Port", Primitive.Int32);

    [Fact]
    public void Primitive_OfADefinedValue_IsTheGivenPrimitive()
    {
        Assert.Same(Port, ConfigurationDefinition.Define("Server:Port", Port).Primitive);
    }

    [Fact]
    public void Primitive_IsKeptByTheBuilderMethods()
    {
        var definition = ConfigurationDefinition.Define("Server:Port", Port).Default(80).Describe("The port.").Sensitive();

        Assert.Same(Port, definition.Primitive);
    }

    [Fact]
    public void Primitive_OfAnIndexedList_IsTheList()
    {
        var origins = new ListPrimitive<Uri>("Origins", Primitive.Uri);

        Assert.Same(origins, ConfigurationDefinition.Indexed("Server:AllowedOrigins", origins).Primitive);
    }

    [Fact]
    public void Primitive_OfAnOptionalValue_IsThePrimitiveOfTheUnderlyingType()
    {
        Assert.Same(Port, ConfigurationDefinition.Define("Server:Port", Port).Optional().Primitive);
        Assert.Same(Primitive.Uri, ConfigurationDefinition.Define("Server:Proxy", Primitive.Uri).Optional().Primitive);
    }

    [Fact]
    public void Primitive_FormatsAValueTheWayTheDocumentationDoes()
    {
        var definition = ConfigurationDefinition.Define("Server:Timeout", Primitive.TimeSpan).Default(TimeSpan.FromSeconds(30));

        var primitive = Assert.IsType<Primitive<TimeSpan>>(definition.Primitive);

        Assert.Equal("00:00:30", primitive.Converter.Format(TimeSpan.FromSeconds(30)));
    }
}
