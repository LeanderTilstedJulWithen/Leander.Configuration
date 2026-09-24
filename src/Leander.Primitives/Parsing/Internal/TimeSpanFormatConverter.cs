using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class TimeSpanFormatConverter(string[] formats, TimeSpanStyles styles) : IConverter<TimeSpan>
{
    public bool TryParse(string input, out TimeSpan result) =>
        TimeSpan.TryParseExact(input, formats, CultureInfo.InvariantCulture, styles, out result);

    public string Format(TimeSpan value) => value.ToString(formats[0], CultureInfo.InvariantCulture);
}
