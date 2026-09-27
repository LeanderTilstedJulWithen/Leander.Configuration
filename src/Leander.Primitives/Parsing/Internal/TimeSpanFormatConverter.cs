using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class TimeSpanFormatConverter(string[] formats, TimeSpanStyles styles) : IConverter<TimeSpan>
{
    private readonly string[] _formats = formats;
    private readonly TimeSpanStyles _styles = styles;

    public bool TryParse(string input, out TimeSpan result) =>
        TimeSpan.TryParseExact(input, _formats, CultureInfo.InvariantCulture, _styles, out result);

    public string Format(TimeSpan value) => value.ToString(_formats[0], CultureInfo.InvariantCulture);
}
