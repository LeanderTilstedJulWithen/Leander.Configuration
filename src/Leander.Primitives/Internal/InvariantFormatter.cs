using System.Globalization;
using Leander.Primitives.Parsing;

namespace Leander.Primitives.Internal;

// Formats a value without a converter, for a rule's Description outside a primitive.
internal sealed class InvariantFormatter<T> : IFormatter<T>
{
    public static readonly InvariantFormatter<T> Instance = new();

    public string Format(T value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
}
