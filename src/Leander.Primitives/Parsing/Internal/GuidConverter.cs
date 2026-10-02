using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class GuidConverter : IConverter<Guid>
{
    public string Description => "A GUID, e.g. 0f8fad5b-d9cb-469f-a165-70867728950e.";

    public bool TryParse(string input, out Guid result) => Guid.TryParse(input, out result);

    public string Format(Guid value) => value.ToString("D", CultureInfo.InvariantCulture);
}
