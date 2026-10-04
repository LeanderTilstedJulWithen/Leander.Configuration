using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class Int64Converter : IConverter<long>
{
    public string Description => "A 64-bit integer.";

    public string Format(long value) => value.ToString(CultureInfo.InvariantCulture);

    public bool TryParse(string input, out long result) =>
        long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
}
