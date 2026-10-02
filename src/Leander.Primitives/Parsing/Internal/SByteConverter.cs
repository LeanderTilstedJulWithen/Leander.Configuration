using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class SByteConverter : IConverter<sbyte>
{
    public string Description => "An 8-bit integer.";

    public bool TryParse(string input, out sbyte result) =>
        sbyte.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    public string Format(sbyte value) => value.ToString(CultureInfo.InvariantCulture);
}
