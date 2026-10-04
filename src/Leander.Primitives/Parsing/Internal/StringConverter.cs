namespace Leander.Primitives.Parsing.Internal;

internal sealed class StringConverter : IConverter<string>
{
    public string Description => "Any text.";

    public string Format(string value) => value;

    public bool TryParse(string input, out string result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        result = input;
        return true;
    }
}
