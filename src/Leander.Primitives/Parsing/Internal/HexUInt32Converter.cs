using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class HexUInt32Converter : IConverter<uint>
{
    public bool TryParse(string input, out uint result)
    {
        var digits = input.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? input[2..] : input;
        return uint.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    public string Format(uint value) => "0x" + value.ToString("X", CultureInfo.InvariantCulture);
}
