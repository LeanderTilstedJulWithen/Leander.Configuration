namespace Leander.Primitives.Parsing.Internal;

internal sealed class StringConverter : IConverter<string>
{
    public bool TryParse(string input, out string result)
    {
        result = input;
        return true;
    }

    public string Format(string value) => value;
}
