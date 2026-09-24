using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class UInt32Converter : IConverter<uint>
{
    public bool TryParse(string input, out uint result) =>
        uint.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    public string Format(uint value) => value.ToString(CultureInfo.InvariantCulture);
}
