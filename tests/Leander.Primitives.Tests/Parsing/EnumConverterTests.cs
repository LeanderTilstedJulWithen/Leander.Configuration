using Leander.Primitives.Parsing;

namespace Leander.Primitives.Tests.Parsing;

public class EnumConverterTests
{
    [Flags]
    public enum Access
    {
        None = 0,
        Read = 1,
        Write = 2,
    }

    [Theory]
    [InlineData("Monday", DayOfWeek.Monday)]
    [InlineData("monday", DayOfWeek.Monday)]
    [InlineData("SUNDAY", DayOfWeek.Sunday)]
    [InlineData("3", DayOfWeek.Wednesday)]
    public void ParsesNamesCaseInsensitivelyAndDefinedNumbers(string input, DayOfWeek expected) =>
        ConverterAssert.Parses(Converters.Enum<DayOfWeek>(), input, expected);

    [Theory]
    [InlineData("")]
    [InlineData("Funday")]
    [InlineData("7")]
    [InlineData("-1")]
    [InlineData("Monday, Tuesday")]
    [InlineData("Wednesday,Wednesday")]
    public void RejectsUndefinedValues(string input) =>
        ConverterAssert.Rejects(Converters.Enum<DayOfWeek>(), input);

    [Theory]
    [InlineData("None", Access.None)]
    [InlineData("Read", Access.Read)]
    [InlineData("read, write", Access.Read | Access.Write)]
    [InlineData("3", Access.Read | Access.Write)]
    public void Flags_ParsesCombinations(string input, Access expected) =>
        ConverterAssert.Parses(Converters.Enum<Access>(), input, expected);

    [Theory]
    [InlineData("4")]
    [InlineData("7")]
    [InlineData("Execute")]
    public void Flags_RejectsUndefinedBits(string input) =>
        ConverterAssert.Rejects(Converters.Enum<Access>(), input);

    [Fact]
    public void FormatsAsName()
    {
        Assert.Equal("Monday", Converters.Enum<DayOfWeek>().Format(DayOfWeek.Monday));
        Assert.Equal("Read, Write", Converters.Enum<Access>().Format(Access.Read | Access.Write));
    }

    [Fact]
    public void ReturnsCachedInstance() =>
        Assert.Same(Converters.Enum<DayOfWeek>(), Converters.Enum<DayOfWeek>());
}
