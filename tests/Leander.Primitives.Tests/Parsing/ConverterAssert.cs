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

    // Rejects the input and returns its reasons, with pieces of the input formatted by formatter (as they are by default).
    public static IReadOnlyList<string> Reasons<T>(IConverter<T> converter, string input, IFormatter<string>? formatter = null)
    {
        Assert.False(converter.TryParse(input, out _, out var errors), $"Expected '{input}' to be rejected.");
        return [.. errors.Select(error => error.FormatWith(formatter ?? PlainFormatter.Instance))];
    }

    public static void RoundTrips<T>(IConverter<T> converter, T value)
    {
        Assert.True(converter.TryParse(converter.Format(value), out var result));
        Assert.Equal(value, result);
    }

    public sealed class HidingFormatter : IFormatter<string>
    {
        public static readonly HidingFormatter Instance = new();

        public string Format(string value) => "***";
    }

    private sealed class PlainFormatter : IFormatter<string>
    {
        public static readonly PlainFormatter Instance = new();

        public string Format(string value) => value;
    }
}
