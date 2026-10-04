using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class Int16Converter : IConverter<short>
{
    public string Description => "A 16-bit integer.";

    public string Format(short value) => value.ToString(CultureInfo.InvariantCulture);

    public bool TryParse(string input, out short result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        return short.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}
