using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class DecimalConverter : IConverter<decimal>
{
    public bool TryParse(string input, out decimal result) =>
        decimal.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    public string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
