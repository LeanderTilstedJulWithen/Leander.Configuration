using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class HexInt32Converter : IConverter<int>
{
    public bool TryParse(string input, out int result)
    {
        var digits = input.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? input[2..] : input;
        return int.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    public string Format(int value) => "0x" + value.ToString("X", CultureInfo.InvariantCulture);
}
