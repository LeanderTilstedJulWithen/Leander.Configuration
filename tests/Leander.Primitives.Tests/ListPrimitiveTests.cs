using Leander.Primitives.Normalization;
using Leander.Primitives.Parsing;
using Leander.Primitives.Validation;

namespace Leander.Primitives.Tests;

public class ListPrimitiveTests
{
    private static readonly Primitive<int> Port = new("Port", Converters.Int32)
    {
        Validators = [Validators.InRange(1, 65535)],
    };

    private static readonly ListPrimitive<int> Ports = new("Ports", Port)
    {
        Validators = [Validators.Collections.NotEmpty<int>()],
    };

    [Fact]
    public void Constructor_HasElementDelimiterAndListType()
    {
        Assert.Same(Port, Ports.Element);
        Assert.Equal(',', Ports.Delimiter);
        Assert.Equal(typeof(IReadOnlyList<int>), Ports.ValueType);
        Assert.Null(Ports.Base);
    }

    [Fact]
    public void TryParse_SplitsOnDelimiterAndTrimsItems()
    {
        Assert.True(Ports.TryParse(" 80, 443 ,8080 ", out var ports));
        Assert.Equal([80, 443, 8080], ports);
    }

    [Fact]
    public void TryParse_CustomDelimiter()
    {
        var ports = new ListPrimitive<int>("Ports", Port, ';');

        Assert.Equal(';', ports.Delimiter);
        Assert.True(ports.TryParse("80;443", out var value));
        Assert.Equal([80, 443], value);
    }

    [Fact]
    public void TryParse_EmptyInput_IsEmptyList()
    {
        var names = new ListPrimitive<string>("Names", Primitive.String);

        Assert.True(names.TryParse("  ", out var value));
        Assert.Empty(value);
    }

    [Fact]
    public void TryParse_AppliesElementRulesToItems()
    {
        var names = new ListPrimitive<string>("Names", new Primitive<string>("Name", Converters.String)
        {
            Normalizers = [Normalizers.Create<string>("upper", value => value.ToUpperInvariant())],
        });

        names.TryParse("a,b", out var value);

        Assert.Equal(["A", "B"], value);
    }

    [Fact]
    public void TryParse_ItemErrors_NameTheItemFromZero()
    {
        Assert.False(Ports.TryParse("80,abc,99999", out _, out var errors));
        Assert.Equal(
            ["item 1: 'abc' is not a valid Int32 (Port)", "item 2: must be between 1 and 65535"],
            errors);
    }

    [Fact]
    public void TryParse_ListRules_RunOnTheList()
    {
        Assert.False(Ports.TryParse("", out _, out var errors));
        Assert.Equal(["must not be empty"], errors);
    }

    [Fact]
    public void TryParse_ItemErrors_SkipListRules()
    {
        var ports = new ListPrimitive<int>("Ports", Port)
        {
            Validators = [Validators.Create<IReadOnlyList<int>>("never", _ => false)],
        };

        ports.TryParse("abc", out _, out var errors);

        Assert.Equal(["item 0: 'abc' is not a valid Int32 (Port)"], errors);
    }

    [Fact]
    public void TryAccept_AcceptsEachItemThenTheList()
    {
        Assert.True(Ports.TryAccept([80, 443], out var accepted));
        Assert.Equal([80, 443], accepted);

        Assert.False(Ports.TryAccept([80, 0], out _, out var itemErrors));
        Assert.Equal(["item 1: must be between 1 and 65535"], itemErrors);

        Assert.False(Ports.TryAccept([], out _, out var listErrors));
        Assert.Equal(["must not be empty"], listErrors);
    }

    [Fact]
    public void Converter_FormatsItemsJoinedByDelimiter()
    {
        var ports = new ListPrimitive<int>("Ports", Port, ';');

        Assert.Equal("80;443", ports.Converter.Format([80, 443]));
    }

    [Fact]
    public void Derived_KeepsElementAndDelimiterAndAddsListRules()
    {
        var ports = new ListPrimitive<int>("Ports", Port, ';');
        var few = new ListPrimitive<int>("FewPorts", ports)
        {
            Validators = [Validators.Create<IReadOnlyList<int>>("at most 2 ports", list => list.Count <= 2)],
        };

        Assert.Same(ports, few.Base);
        Assert.Same(Port, few.Element);
        Assert.Equal(';', few.Delimiter);
        Assert.False(few.TryParse("1;2;3", out _, out var errors));
        Assert.Equal(["at most 2 ports"], errors);
        Assert.False(few.TryParse("1;0", out _, out var itemErrors));
        Assert.Equal(["item 1: must be between 1 and 65535"], itemErrors);
    }

    [Fact]
    public void DerivedAsPlainPrimitive_StillChecksItemsWithoutNamingThem()
    {
        var plain = new Primitive<IReadOnlyList<int>>("PlainPorts", Ports);

        Assert.True(plain.TryParse("80,443", out var value));
        Assert.Equal([80, 443], value);
        Assert.False(plain.TryParse("80,0", out _, out var errors));
        Assert.Equal(["'80,0' is not a valid IReadOnlyList<Int32> (PlainPorts)"], errors);
    }

    [Fact]
    public void Nested_ListOfLists()
    {
        var row = new ListPrimitive<int>("Row", Primitive.Int32);
        var grid = new ListPrimitive<IReadOnlyList<int>>("Grid", row, ';');

        Assert.True(grid.TryParse("1,2;3", out var value));
        Assert.Equal(2, value.Count);
        Assert.Equal([1, 2], value[0]);
        Assert.Equal([3], value[1]);

        grid.TryParse("1;2,x", out _, out var errors);
        Assert.Equal(["item 1: item 1: 'x' is not a valid Int32"], errors);
    }
}
