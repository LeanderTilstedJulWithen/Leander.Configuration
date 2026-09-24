using System.Globalization;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Parsing;

public sealed class DateTimeOffsetConverterBuilder
{
    private readonly List<string> _formats = [];
    private DateTimeStyles _styles = DateTimeStyles.None;

    public DateTimeOffsetConverterBuilder AddFormat(string format)
    {
        _formats.Add(format);
        return this;
    }

    public DateTimeOffsetConverterBuilder WithStyles(DateTimeStyles styles)
    {
        _styles = styles;
        return this;
    }

    public IConverter<DateTimeOffset> Build() => new DateTimeOffsetFormatConverter(_formats.ToArray(), _styles);
}
