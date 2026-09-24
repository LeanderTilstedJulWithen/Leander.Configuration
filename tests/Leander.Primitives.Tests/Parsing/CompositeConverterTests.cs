using Leander.Primitives.Parsing;

namespace Leander.Primitives.Tests.Parsing;

public class CompositeConverterTests
{
    public sealed record Point(int X, int Y);

    [Fact]
    public void List_ParsesDelimitedValuesAndTrimsSegments()
    {
        var converter = Converters.List(Converters.Int32);

        Assert.True(converter.TryParse(" 1, 2 ,3 ", out var result));
        Assert.Equal([1, 2, 3], result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void List_ParsesBlankInputAsEmpty(string input)
    {
        Assert.True(Converters.List(Converters.Int32).TryParse(input, out var result));
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("1,x,3")]
    [InlineData("1,,3")]
    [InlineData("1,")]
    public void List_RejectsInvalidElementAndReturnsEmptyResult(string input)
    {
        Assert.False(Converters.List(Converters.Int32).TryParse(input, out var result));
        Assert.Empty(result);
    }

    [Fact]
    public void List_UsesCustomDelimiter()
    {
        var converter = Converters.List(Converters.String, ';');

        Assert.True(converter.TryParse("a,b;c", out var result));
        Assert.Equal(["a,b", "c"], result);
        Assert.Equal("a,b;c", converter.Format(result));
    }

    [Fact]
    public void List_FormatsWithElementConverter()
    {
        var converter = Converters.List(Converters.Int32Hex);

        Assert.Equal("0x1,0xFF", converter.Format([1, 255]));
        ConverterAssert.RoundTrips<IReadOnlyList<int>>(converter, [1, 255]);
    }

    [Fact]
    public void Dictionary_ParsesEntriesAndTrimsKeysAndValues()
    {
        var converter = Converters.Dictionary(Converters.String, Converters.Int32);

        Assert.True(converter.TryParse(" a = 1, b=2 ", out var result));
        Assert.Equal(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }, result);
    }

    [Fact]
    public void Dictionary_SplitsOnlyOnFirstKeyValueDelimiter()
    {
        var converter = Converters.Dictionary(Converters.String, Converters.String);

        Assert.True(converter.TryParse("a=b=c", out var result));
        Assert.Equal("b=c", result["a"]);
    }

    [Fact]
    public void Dictionary_ParsesBlankInputAsEmpty()
    {
        Assert.True(Converters.Dictionary(Converters.String, Converters.Int32).TryParse(" ", out var result));
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("a=1,b")]
    [InlineData("a=x")]
    [InlineData("a=1,a=2")]
    public void Dictionary_RejectsInvalidEntriesAndReturnsEmptyResult(string input)
    {
        Assert.False(Converters.Dictionary(Converters.String, Converters.Int32).TryParse(input, out var result));
        Assert.Empty(result);
    }

    [Fact]
    public void Dictionary_UsesCustomDelimiters()
    {
        var converter = Converters.Dictionary(Converters.String, Converters.Int32, ';', ':');

        Assert.True(converter.TryParse("a:1;b:2", out var result));
        Assert.Equal(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }, result);
        Assert.Equal("a:1;b:2", converter.Format(result));
    }

    [Fact]
    public void Json_ParsesAndFormats()
    {
        var converter = Converters.Json<Point>();

        ConverterAssert.Parses(converter, """{"X":1,"Y":2}""", new Point(1, 2));
        Assert.Equal("""{"X":1,"Y":2}""", converter.Format(new Point(1, 2)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("""{"X":"one"}""")]
    public void Json_RejectsInvalidJson(string input) =>
        ConverterAssert.Rejects(Converters.Json<Point>(), input);
}
