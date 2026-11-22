using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class DateTimeFormatConverter(string[] formats, DateTimeStyles styles, TimeZoneInfo? timeZone, string description)
    : IConverter<DateTime>
{
    private readonly string _description = description;
    private readonly string[] _formats = formats;
    private readonly DateTimeStyles _styles = styles;
    private readonly TimeZoneInfo? _timeZone = timeZone;
    private readonly DateTimeKind _zoneKind = KindOf(timeZone);

    public string Description => _description;

    public string Format(DateTime value) => InZone(value).ToString(_formats[0], CultureInfo.InvariantCulture);

    public bool TryParse(string input, out DateTime result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        return DateTime.TryParseExact(input, _formats, CultureInfo.InvariantCulture, _styles, out result);
    }

    // The kind a DateTime in the zone has, so a K in the format writes Z for UTC and the offset for local time.
    private static DateTimeKind KindOf(TimeZoneInfo? timeZone) =>
        timeZone is null ? DateTimeKind.Unspecified
        : timeZone.Equals(TimeZoneInfo.Utc) ? DateTimeKind.Utc
        : timeZone.Equals(TimeZoneInfo.Local) ? DateTimeKind.Local
        : DateTimeKind.Unspecified;

    // An unspecified value is taken as being in the zone already, the way text without an offset is read with AssumeUniversal.
    private DateTime InZone(DateTime value) =>
        _timeZone is null ? value
        : value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, _zoneKind)
        : TimeZoneInfo.ConvertTime(value, _timeZone);
}
