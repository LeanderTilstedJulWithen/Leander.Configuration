using Leander.Primitives.Internal;
using Leander.Primitives.Parsing;

namespace Leander.Primitives;

public static class FormattableText
{
    // Text without values of T.
    public static IFormattableText<T> Create<T>(string text) => new DelegateText<T>(_ => text);

    // Text with values of T, formatted by the given formatter: formatter => $"must be at most {formatter.Format(max)}".
    public static IFormattableText<T> Create<T>(Func<IFormatter<T>, string> format) => new DelegateText<T>(format);
}
