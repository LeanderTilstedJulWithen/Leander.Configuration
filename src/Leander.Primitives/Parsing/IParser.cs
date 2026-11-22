using System.Diagnostics.CodeAnalysis;

namespace Leander.Primitives.Parsing;

/// <summary>
/// Parses strings into values of <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// On failure, <c>result</c> is <see langword="default"/>, <see langword="null"/> for a reference type: a parser
/// doesn't have to make up a value, and a caller doesn't use it.
/// </remarks>
public interface IParser<T>
{
    /// <summary>
    /// Parses <paramref name="input"/>; returns <see langword="false"/> if it isn't a valid value.
    /// </summary>
    /// <remarks>
    /// The default calls the overload with errors. Override it only for speed.
    /// </remarks>
    public bool TryParse(string input, [MaybeNullWhen(false)] out T result) => TryParse(input, out result, out _);

    /// <summary>
    /// Parses <paramref name="input"/>; returns <see langword="false"/> if it isn't a valid value,
    /// with every reason in <paramref name="errors"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <paramref name="errors"/> is empty on success, and may be empty on failure too: the primitive leads with its own line,
    /// e.g. "'abc' is not a valid Port", and adds each reason to it.
    /// </para>
    /// <para>
    /// Pieces of the input in a reason go through the formatter, e.g. <c>formatter => $"'{formatter.Format(segment)}' is not a number"</c>,
    /// so the primitive can hide them for a sensitive value.
    /// </para>
    /// </remarks>
    public bool TryParse(string input, [MaybeNullWhen(false)] out T result, out IReadOnlyList<IFormattableText<string>> errors);
}
