using System.Globalization;

namespace Leander.Primitives.Parsing.Internal;

internal sealed class TimeSpanFormatConverter(string[] formats, TimeSpanStyles styles, string description) : IConverter<TimeSpan>
{
    private readonly string _description = description;
    private readonly string[] _formats = formats;
    private readonly TimeSpanStyles _styles = styles;

    public string Description => _description;

    public string Format(TimeSpan value) => value.ToString(_formats[0], CultureInfo.InvariantCulture);

    public bool TryParse(string input, out TimeSpan result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        return TimeSpan.TryParseExact(input, _formats, CultureInfo.InvariantCulture, _styles, out result);
    }
}
