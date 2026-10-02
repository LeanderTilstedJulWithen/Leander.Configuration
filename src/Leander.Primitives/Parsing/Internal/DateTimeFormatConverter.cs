using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class DateTimeFormatConverter(string[] formats, DateTimeStyles styles, string description) : IConverter<DateTime>
{
    private readonly string _description = description;
    private readonly string[] _formats = formats;
    private readonly DateTimeStyles _styles = styles;

    public string Description => _description;

    public bool TryParse(string input, out DateTime result) =>
        DateTime.TryParseExact(input, _formats, CultureInfo.InvariantCulture, _styles, out result);

    public string Format(DateTime value) => value.ToString(_formats[0], CultureInfo.InvariantCulture);
}
