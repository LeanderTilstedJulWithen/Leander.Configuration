using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives.Tests.Parsing;

public class CompositeConverterTests
{
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
    public void List_ValidInput_HasNoErrors()
    {
        Assert.True(Converters.List(Converters.Int32).TryParse("1,2", out _, out var errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void List_InvalidItems_ReportsEveryItem()
    {
        var reasons = ConverterAssert.Reasons(Converters.List(Converters.Int32), "1,x,3,y");

        Assert.Equal(["item 1: 'x' is not a valid Int32", "item 3: 'y' is not a valid Int32"], reasons);
    }

    [Fact]
    public void List_InvalidItem_GoesThroughTheFormatterWithItsQuotes()
    {
        var reasons = ConverterAssert.Reasons(Converters.List(Converters.Int32), "1,x", ConverterAssert.HidingFormatter.Instance);

        Assert.Equal(["item 1: *** is not a valid Int32"], reasons);
    }

    [Fact]
    public void List_ElementReasons_FollowTheItem()
    {
        var port = new Primitive<int>("Port", Converters.Int32) { Validators = [Validators.InRange(1, 65535)] };

        var reasons = ConverterAssert.Reasons(Converters.List(port.AsConverter()), "80,0,x");

        Assert.Equal(["item 1: must be between 1 and 65535", "item 2: 'x' is not a valid Int32 (Port)"], reasons);
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

    [Theory]
    [InlineData("a", "entry 0: 'a' has no '='")]
    [InlineData("a=1, x=y", "entry 1: value: 'y' is not a valid Int32")]
    [InlineData("a=1,a=2", "entry 1: duplicate key 'a'")]
    public void Dictionary_InvalidEntry_SaysWhy(string input, string expected)
    {
        var reasons = ConverterAssert.Reasons(Converters.Dictionary(Converters.String, Converters.Int32), input);

        Assert.Equal([expected], reasons);
    }

    [Fact]
    public void Dictionary_InvalidKeyAndValue_ReportsBoth()
    {
        var reasons = ConverterAssert.Reasons(Converters.Dictionary(Converters.Int32, Converters.Int32), "1=2,x=y");

        Assert.Equal(["entry 1: key: 'x' is not a valid Int32", "entry 1: value: 'y' is not a valid Int32"], reasons);
    }

    [Fact]
    public void Dictionary_InvalidEntries_ReportsEveryEntry()
    {
        var reasons = ConverterAssert.Reasons(Converters.Dictionary(Converters.String, Converters.Int32), "a,b=1,c=x");

        Assert.Equal(["entry 0: 'a' has no '='", "entry 2: value: 'x' is not a valid Int32"], reasons);
    }

    [Fact]
    public void Dictionary_InvalidEntry_GoesThroughTheFormatter()
    {
        var reasons = ConverterAssert.Reasons(
            Converters.Dictionary(Converters.String, Converters.Int32),
            "a,a=1,a=2",
            ConverterAssert.HidingFormatter.Instance);

        Assert.Equal(["entry 0: *** has no '='", "entry 2: duplicate key ***"], reasons);
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

    public sealed record Point(int X, int Y);
}
