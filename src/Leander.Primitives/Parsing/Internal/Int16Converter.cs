using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class Int16Converter : IConverter<short>
{
    public bool TryParse(string input, out short result) =>
        short.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    public string Format(short value) => value.ToString(CultureInfo.InvariantCulture);
}
