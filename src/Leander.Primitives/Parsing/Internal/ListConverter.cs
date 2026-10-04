namespace Leander.Primitives.Parsing.Internal;

internal sealed class ListConverter<T>(IConverter<T> elementConverter, char delimiter) : IConverter<IReadOnlyList<T>>
{
    private readonly char _delimiter = delimiter;
    private readonly IConverter<T> _elementConverter = elementConverter;

    public string Description => _elementConverter.Description is { } element
        ? $"A list separated by '{_delimiter}'. Each item: {element}"
        : $"A list separated by '{_delimiter}'.";

    public string Format(IReadOnlyList<T> value) => string.Join(_delimiter, value.Select(_elementConverter.Format));

    // Every item is checked, and every item's reasons are reported, e.g. "item 2: 'x' is not a valid Int32".
    public bool TryParse(string input, out IReadOnlyList<T> result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        var list = new List<T>();
        var reasons = new List<IFormattableText<string>>();
        var segments = Split(input, _delimiter);

        for (var index = 0; index < segments.Length; index++)
        {
            if (_elementConverter.TryParse(segments[index], out var element, out var itemErrors))
            {
                list.Add(element);
                continue;
            }

            reasons.AddRange(PartErrors.Of<T>($"item {index}: ", segments[index], itemErrors));
        }

        errors = reasons;
        result = reasons.Count == 0 ? list : [];
        return reasons.Count == 0;
    }

    // The trimmed items of a delimited list. Empty input is an empty list.
    internal static string[] Split(string input, char delimiter)
    {
        var trimmed = input.Trim();
        return trimmed.Length == 0 ? [] : [.. trimmed.Split(delimiter).Select(segment => segment.Trim())];
    }
}
