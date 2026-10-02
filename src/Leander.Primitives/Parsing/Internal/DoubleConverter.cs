using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class DoubleConverter : IConverter<double>
{
    public string Description => "A 64-bit floating-point number, with . as the decimal separator.";

    public bool TryParse(string input, out double result) =>
        double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    public string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
