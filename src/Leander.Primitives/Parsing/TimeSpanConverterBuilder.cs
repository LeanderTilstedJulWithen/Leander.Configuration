using System.Globalization;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Parsing;

/// <summary>
/// Builds a <see cref="TimeSpan"/> converter from exact formats, in the invariant culture.
/// </summary>
public sealed class TimeSpanConverterBuilder
{
    private readonly List<string> _formats = [];
    private TimeSpanStyles _styles = TimeSpanStyles.None;

    /// <summary>
    /// Adds a format the converter accepts. The first format is also the one values are formatted with.
    /// </summary>
    public TimeSpanConverterBuilder AddFormat(string format)
    {
        _formats.Add(format);
        return this;
    }

    /// <summary>
    /// Sets the styles used when parsing.
    /// </summary>
    public TimeSpanConverterBuilder WithStyles(TimeSpanStyles styles)
    {
        _styles = styles;
        return this;
    }

    /// <summary>
    /// Builds the converter. Add at least one format first.
    /// </summary>
    public IConverter<TimeSpan> Build() => new TimeSpanFormatConverter(_formats.ToArray(), _styles);
}
