namespace Leander.Primitives.Parsing.Internal;

internal sealed class BooleanConverter : IConverter<bool>
{
    public string Description => "Either true or false, ignoring case.";

    public string Format(bool value) => value.ToString();

    public bool TryParse(string input, out bool result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        return bool.TryParse(input, out result);
    }
}
