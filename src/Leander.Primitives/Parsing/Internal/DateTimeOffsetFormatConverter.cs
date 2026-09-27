using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class DateTimeOffsetFormatConverter(string[] formats, DateTimeStyles styles) : IConverter<DateTimeOffset>
{
    private readonly string[] _formats = formats;
    private readonly DateTimeStyles _styles = styles;

    public bool TryParse(string input, out DateTimeOffset result) =>
        DateTimeOffset.TryParseExact(input, _formats, CultureInfo.InvariantCulture, _styles, out result);

    public string Format(DateTimeOffset value) => value.ToString(_formats[0], CultureInfo.InvariantCulture);
}
