using Leander.Primitives.Parsing;

namespace Leander.Primitives;

/// <summary>
/// A rule's description or failure, not yet formatted.
/// </summary>
/// <remarks>
/// Values of <typeparamref name="T"/> in it, e.g. a bound, are formatted by the primitive with its converter
/// ("must be less than or equal to 0xFF" on a Hex primitive), or hidden for a sensitive value.
/// The primitive turns it into a string, so only rule authors see this type.
/// Create one with <see cref="FormattableText.Create{T}(string)"/>.
/// </remarks>
// Covariant, because it only hands values of T to the formatter.
public interface IFormattableText<out T>
{
    /// <summary>
    /// Formats the text, with values of <typeparamref name="T"/> formatted by <paramref name="formatter"/>.
    /// </summary>
    public string FormatWith(IFormatter<T> formatter);
}
