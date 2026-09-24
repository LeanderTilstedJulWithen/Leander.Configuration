namespace Leander.Primitives.Parsing.Internal;

internal sealed class BooleanConverter : IConverter<bool>
{
    public bool TryParse(string input, out bool result) => bool.TryParse(input, out result);

    public string Format(bool value) => value.ToString();
}
