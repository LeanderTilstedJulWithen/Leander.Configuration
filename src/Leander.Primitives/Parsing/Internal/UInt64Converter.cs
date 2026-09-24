using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class UInt64Converter : IConverter<ulong>
{
    public bool TryParse(string input, out ulong result) =>
        ulong.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    public string Format(ulong value) => value.ToString(CultureInfo.InvariantCulture);
}
