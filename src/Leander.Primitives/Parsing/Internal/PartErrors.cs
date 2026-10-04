using Leander.Primitives.Internal;

namespace Leander.Primitives.Parsing.Internal;

// Errors of a part of the input, e.g. a list item, for composite converters.
internal static class PartErrors
{
    // The part's reasons, each after the prefix, e.g. "item 2: "; without reasons, one line that names the part's type.
    // The part goes through the formatter with its quotes, so it reads "item 2: (hidden) is not a valid Int32" when hidden.
    public static IEnumerable<IFormattableText<string>> Of<T>(string prefix, string part, IReadOnlyList<IFormattableText<string>> reasons) =>
        reasons.Count == 0
            ? [FormattableText.Create<string>(formatter => $"{prefix}{formatter.Format($"'{part}'")} is not a valid {TypeNames.Get(typeof(T))}")]
            : reasons.Select(reason => FormattableText.Create<string>(formatter => prefix + reason.FormatWith(formatter)));
}
