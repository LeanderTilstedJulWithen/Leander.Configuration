using System.Globalization;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Parsing;

/// <summary>
/// Builds a <see cref="DateTimeOffset"/> converter from exact formats, in the invariant culture.
/// </summary>
public sealed class DateTimeOffsetConverterBuilder
{
    private readonly List<string> _formats = [];
    private DateTimeStyles _styles = DateTimeStyles.None;

    /// <summary>
    /// Adds a format the converter accepts. The first format is also the one values are formatted with.
    /// </summary>
    public DateTimeOffsetConverterBuilder AddFormat(string format)
    {
        _formats.Add(format);
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

    /// <summary>
    /// Builds the converter. Add at least one format first.
    /// </summary>
    public IConverter<DateTimeOffset> Build() => new DateTimeOffsetFormatConverter(_formats.ToArray(), _styles);
}
