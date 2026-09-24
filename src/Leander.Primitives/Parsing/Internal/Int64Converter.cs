using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class Int64Converter : IConverter<long>
{
    public bool TryParse(string input, out long result) =>
        long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    public string Format(long value) => value.ToString(CultureInfo.InvariantCulture);
}
