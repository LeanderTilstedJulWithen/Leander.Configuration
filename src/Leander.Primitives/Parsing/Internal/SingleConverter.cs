using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class SingleConverter : IConverter<float>
{
    public string Description => "A 32-bit floating-point number, with . as the decimal separator.";

    public string Format(float value) => value.ToString(CultureInfo.InvariantCulture);

    public bool TryParse(string input, out float result) =>
        float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
}
