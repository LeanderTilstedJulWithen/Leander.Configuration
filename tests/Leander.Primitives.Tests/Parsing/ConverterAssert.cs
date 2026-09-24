using Leander.Primitives.Parsing;

namespace Leander.Primitives.Tests.Parsing;

internal static class ConverterAssert
{
    public static void Parses<T>(IConverter<T> converter, string input, T expected)
    {
        Assert.True(converter.TryParse(input, out var result), $"Expected '{input}' to parse.");
        Assert.Equal(expected, result);
    }

    public static void Rejects<T>(IConverter<T> converter, string input)
    {
        Assert.False(converter.TryParse(input, out _), $"Expected '{input}' to be rejected.");
    }

    public static void RoundTrips<T>(IConverter<T> converter, T value)
    {
        Assert.True(converter.TryParse(converter.Format(value), out var result));
        Assert.Equal(value, result);
    }
}
