using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class SingleConverter : IConverter<float>
{
    public bool TryParse(string input, out float result) =>
        float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    public string Format(float value) => value.ToString(CultureInfo.InvariantCulture);
}
