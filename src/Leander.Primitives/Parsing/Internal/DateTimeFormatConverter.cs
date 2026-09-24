using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class DateTimeFormatConverter(string[] formats, DateTimeStyles styles) : IConverter<DateTime>
{
    public bool TryParse(string input, out DateTime result) =>
        DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, styles, out result);

    public string Format(DateTime value) => value.ToString(formats[0], CultureInfo.InvariantCulture);
}
