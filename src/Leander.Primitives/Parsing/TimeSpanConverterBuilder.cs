using System.Globalization;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Parsing;

public sealed class TimeSpanConverterBuilder
{
    private readonly List<string> _formats = [];
    private TimeSpanStyles _styles = TimeSpanStyles.None;

    public TimeSpanConverterBuilder AddFormat(string format)
    {
        _formats.Add(format);
        return this;
    }

    public TimeSpanConverterBuilder WithStyles(TimeSpanStyles styles)
    {
        _styles = styles;
        return this;
    }

    public IConverter<TimeSpan> Build() => new TimeSpanFormatConverter(_formats.ToArray(), _styles);
}
