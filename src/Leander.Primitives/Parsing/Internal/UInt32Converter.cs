using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class UInt32Converter : IConverter<uint>
{
    public string Description => "A 32-bit unsigned integer.";

    public bool TryParse(string input, out uint result) =>
        uint.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    public string Format(uint value) => value.ToString(CultureInfo.InvariantCulture);
}
