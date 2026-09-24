using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class IntConverter : IConverter<int>
{
    public bool TryParse(string input, out int result) =>
        int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    public string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
