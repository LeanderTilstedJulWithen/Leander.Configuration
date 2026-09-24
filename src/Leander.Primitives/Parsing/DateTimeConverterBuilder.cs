using System.Globalization;
using Leander.Primitives.Parsing.Internal;

namespace Leander.Primitives.Parsing;

public sealed class DateTimeConverterBuilder
{
    private readonly List<string> _formats = [];
    private DateTimeStyles _styles = DateTimeStyles.None;

    public DateTimeConverterBuilder AddFormat(string format)
    {
        _formats.Add(format);
        return this;
    }

    public DateTimeConverterBuilder WithStyles(DateTimeStyles styles)
    {
        _styles = styles;
        return this;
    }

    public IConverter<DateTime> Build() => new DateTimeFormatConverter(_formats.ToArray(), _styles);
}
