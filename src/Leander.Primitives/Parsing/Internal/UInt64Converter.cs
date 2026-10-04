using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class UInt64Converter : IConverter<ulong>
{
    public string Description => "A 64-bit unsigned integer.";

    public string Format(ulong value) => value.ToString(CultureInfo.InvariantCulture);

    public bool TryParse(string input, out ulong result) =>
        ulong.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
}
