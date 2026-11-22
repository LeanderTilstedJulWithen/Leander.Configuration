using System.Globalization;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Parsing;

/// <summary>
/// Builds a <see cref="DateTimeOffset"/> converter from exact formats, in the invariant culture.
/// </summary>
public sealed class DateTimeOffsetConverterBuilder
{
    private readonly List<string> _formats = [];
    private string? _description;
    private DateTimeStyles _styles = DateTimeStyles.None;
    private TimeZoneInfo? _timeZone;

    /// <summary>
    /// Adds a format the converter accepts. The first format is also the one values are formatted with.
    /// </summary>
    public DateTimeOffsetConverterBuilder AddFormat(string format)
    {
        _formats.Add(format);
        return this;
    }

    /// <summary>
    /// Builds the converter. Add at least one format first.
    /// </summary>
    public IConverter<DateTimeOffset> Build()
    {
        var formats = _formats.ToArray();
        return new DateTimeOffsetFormatConverter(
            formats,
            _styles,
            _timeZone,
            _description ?? FormatDescriptions.DateTime(formats, _styles, _timeZone));
    }

    /// <summary>
    /// Converts a value to <paramref name="timeZone"/> before formatting it, e.g. <see cref="TimeZoneInfo.Utc"/>, so a
    /// literal <c>'Z'</c> in the format is true. Without it, a value is written with its own offset.
    /// </summary>
    /// <remarks>
    /// Styles only apply to parsing, so <see cref="DateTimeStyles.AdjustToUniversal"/> reads into UTC, and this writes in UTC.
    /// </remarks>
    public DateTimeOffsetConverterBuilder FormatIn(TimeZoneInfo timeZone)
    {
        _timeZone = timeZone;
        return this;
    }

    /// <summary>
    /// Sets the converter's description, instead of the one generated from the formats and styles.
    /// </summary>
    public DateTimeOffsetConverterBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the styles used when parsing, e.g. <see cref="DateTimeStyles.AssumeUniversal"/>.
    /// </summary>
    public DateTimeOffsetConverterBuilder WithStyles(DateTimeStyles styles)
    {
        _styles = styles;
        return this;
    }
}
