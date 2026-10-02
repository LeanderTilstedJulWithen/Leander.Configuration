using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class DecimalConverter : IConverter<decimal>
{
    public string Description => "A decimal number, with . as the decimal separator.";

    public bool TryParse(string input, out decimal result) =>
        decimal.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    public string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
