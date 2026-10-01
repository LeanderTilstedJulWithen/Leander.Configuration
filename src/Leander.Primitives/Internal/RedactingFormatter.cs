using Leander.Primitives.Parsing;

namespace Leander.Primitives.Internal;

// Hides every value of T in a rule's text, for sensitive values. Conservative: bounds are hidden too,
// because a text can't tell a bound from the checked value.
internal sealed class RedactingFormatter<T> : IFormatter<T>
{
    public static readonly RedactingFormatter<T> Instance = new();

    public string Format(T value) => "(hidden)";
}
