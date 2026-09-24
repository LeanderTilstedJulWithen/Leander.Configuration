using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class DoubleConverter : IConverter<double>
{
    public bool TryParse(string input, out double result) =>
        double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    public string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
