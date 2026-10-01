using Leander.Primitives.Internal;
using Leander.Primitives.Parsing;

namespace Leander.Primitives;

/// <summary>
/// Creates <see cref="IFormattableText{T}"/> for rule descriptions and failures.
/// </summary>
public static class FormattableText
{
    /// <summary>
    /// Creates text without values of <typeparamref name="T"/>.
    /// </summary>
    public static IFormattableText<T> Create<T>(string text) => new DelegateText<T>(_ => text);

    /// <summary>
    /// Creates text with values of <typeparamref name="T"/>, formatted by the formatter it is given,
    /// e.g. <c>formatter => $"must be at most {formatter.Format(max)}"</c>.
    /// </summary>
    public static IFormattableText<T> Create<T>(Func<IFormatter<T>, string> format) => new DelegateText<T>(format);
}
