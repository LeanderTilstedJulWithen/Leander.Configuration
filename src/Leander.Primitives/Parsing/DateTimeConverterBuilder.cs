using System.Globalization;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Parsing;

/// <summary>
/// Builds a <see cref="DateTime"/> converter from exact formats, in the invariant culture.
/// </summary>
public sealed class DateTimeConverterBuilder
{
    private readonly List<string> _formats = [];
    private DateTimeStyles _styles = DateTimeStyles.None;

    /// <summary>
    /// Adds a format the converter accepts. The first format is also the one values are formatted with.
    /// </summary>
    public DateTimeConverterBuilder AddFormat(string format)
    {
        _formats.Add(format);
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

    /// <summary>
    /// Builds the converter. Add at least one format first.
    /// </summary>
    public IConverter<DateTime> Build() => new DateTimeFormatConverter(_formats.ToArray(), _styles);
}
