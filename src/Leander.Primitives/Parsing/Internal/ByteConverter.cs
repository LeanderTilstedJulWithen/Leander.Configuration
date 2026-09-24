using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class ByteConverter : IConverter<byte>
{
    public bool TryParse(string input, out byte result) =>
        byte.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    public string Format(byte value) => value.ToString(CultureInfo.InvariantCulture);
}
