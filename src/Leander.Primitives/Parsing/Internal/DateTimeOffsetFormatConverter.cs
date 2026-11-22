using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class DateTimeOffsetFormatConverter(string[] formats, DateTimeStyles styles, TimeZoneInfo? timeZone, string description)
    : IConverter<DateTimeOffset>
{
    private readonly string _description = description;
    private readonly string[] _formats = formats;
    private readonly DateTimeStyles _styles = styles;
    private readonly TimeZoneInfo? _timeZone = timeZone;

    public string Description => _description;

    public string Format(DateTimeOffset value) =>
        (_timeZone is null ? value : TimeZoneInfo.ConvertTime(value, _timeZone)).ToString(_formats[0], CultureInfo.InvariantCulture);

    public bool TryParse(string input, out DateTimeOffset result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        return DateTimeOffset.TryParseExact(input, _formats, CultureInfo.InvariantCulture, _styles, out result);
    }
}
