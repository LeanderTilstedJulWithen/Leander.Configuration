using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class DateTimeOffsetFormatConverter(string[] formats, DateTimeStyles styles) : IConverter<DateTimeOffset>
{
    public bool TryParse(string input, out DateTimeOffset result) =>
        DateTimeOffset.TryParseExact(input, formats, CultureInfo.InvariantCulture, styles, out result);

    public string Format(DateTimeOffset value) => value.ToString(formats[0], CultureInfo.InvariantCulture);
}
