using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class UInt16Converter : IConverter<ushort>
{
    public string Description => "A 16-bit unsigned integer.";

    public string Format(ushort value) => value.ToString(CultureInfo.InvariantCulture);

    public bool TryParse(string input, out ushort result) =>
        ushort.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
}
