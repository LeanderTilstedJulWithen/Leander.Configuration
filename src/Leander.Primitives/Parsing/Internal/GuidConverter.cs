using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class GuidConverter : IConverter<Guid>
{
    public bool TryParse(string input, out Guid result) => Guid.TryParse(input, out result);

    public string Format(Guid value) => value.ToString("D", CultureInfo.InvariantCulture);
}
