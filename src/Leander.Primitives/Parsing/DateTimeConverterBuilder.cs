using System.Globalization;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Parsing;

/// <summary>
/// Builds a <see cref="DateTime"/> converter from exact formats, in the invariant culture.
/// </summary>
public sealed class DateTimeConverterBuilder
{
    private readonly List<string> _formats = [];
    private string? _description;
    private DateTimeStyles _styles = DateTimeStyles.None;
    private TimeZoneInfo? _timeZone;

    /// <summary>
    /// Adds a format the converter accepts. The first format is also the one values are formatted with.
    /// </summary>
    public DateTimeConverterBuilder AddFormat(string format)
    {
        _formats.Add(format);
        return this;
    }

    /// <summary>
    /// Builds the converter. Add at least one format first.
    /// </summary>
    public IConverter<DateTime> Build()
    {
        var formats = _formats.ToArray();
        return new DateTimeFormatConverter(
            formats,
            _styles,
            _timeZone,
            _description ?? FormatDescriptions.DateTime(formats, _styles, _timeZone));
    }

    /// <summary>
    /// Converts a value to <paramref name="timeZone"/> before formatting it, e.g. <see cref="TimeZoneInfo.Utc"/>.
    /// Without it, a value is written as it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Styles only apply to parsing, so <see cref="DateTimeStyles.AdjustToUniversal"/> reads into UTC, and this writes in UTC.
    /// </para>
    /// <para>
    /// A value whose <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Unspecified"/>, e.g. a default written in code,
    /// is taken as being in the zone already. For UTC and local time it gets that kind, so a <c>K</c> in the format writes
    /// <c>Z</c> or the local offset. A <see cref="DateTime"/> in any other zone has no kind for it, and <c>K</c> writes nothing.
    /// </para>
    /// </remarks>
    public DateTimeConverterBuilder FormatIn(TimeZoneInfo timeZone)
    {
        _timeZone = timeZone;
        return this;
    }

    /// <summary>
    /// Sets the converter's description, instead of the one generated from the formats and styles.
    /// </summary>
    public DateTimeConverterBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the styles used when parsing, e.g. <see cref="DateTimeStyles.AssumeUniversal"/>.
    /// </summary>
    public DateTimeConverterBuilder WithStyles(DateTimeStyles styles)
    {
        _styles = styles;
        return this;
    }
}
