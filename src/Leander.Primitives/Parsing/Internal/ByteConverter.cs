using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class ByteConverter : IConverter<byte>
{
    public string Description => "An 8-bit unsigned integer.";

    public string Format(byte value) => value.ToString(CultureInfo.InvariantCulture);

    public bool TryParse(string input, out byte result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        return byte.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}
