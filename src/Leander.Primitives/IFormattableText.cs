using Leander.Primitives.Parsing;

namespace Leander.Primitives;

// A rule's description or failure, not yet formatted: values of T in it, e.g. a bound, are formatted by the primitive
// with its converter ("must be less than or equal to 0xFF" on a Hex primitive), or hidden for a sensitive value.
// The primitive turns it into a string, so only rule authors see this type. Create one with FormattableText.Create.
// Covariant, because it only hands values of T to the formatter.
public interface IFormattableText<out T>
{
    public string FormatWith(IFormatter<T> formatter);
}
