using Leander.Primitives.Parsing;

namespace Leander.Primitives.Internal;

// An error in both forms: as shown, and with the value hidden, for a sensitive value. Both are formatted where the error
// occurs, e.g. inside the rules' try/catch, and the boundary picks one. As text, a redacting formatter picks the hidden form,
// so a converter that wraps a primitive passes its errors on, and the outer primitive decides.
internal sealed class ErrorText(string shown, string hidden) : IFormattableText<string>
{
    private readonly string _hidden = hidden;
    private readonly string _shown = shown;

    // Both forms of a converter's reason: pieces of the input as they are, or hidden.
    public static ErrorText From(IFormattableText<string> text) =>
        text as ErrorText ?? new(text.FormatWith(InvariantFormatter<string>.Instance), text.FormatWith(RedactingFormatter<string>.Instance));

    public static IReadOnlyList<string> Pick(IEnumerable<ErrorText> errors, bool redact) =>
        [.. errors.Select(error => error.Pick(redact))];

    public string FormatWith(IFormatter<string> formatter) => Pick(formatter is RedactingFormatter<string>);

    public string Pick(bool redact) => redact ? _hidden : _shown;

    // E.g. "item 2: " before an item's error.
    public ErrorText WithPrefix(string prefix) => WithPrefix(prefix, prefix);

    public ErrorText WithPrefix(string shown, string hidden) => new(shown + _shown, hidden + _hidden);
}
