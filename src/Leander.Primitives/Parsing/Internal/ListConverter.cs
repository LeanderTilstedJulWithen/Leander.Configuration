namespace Leander.Primitives.Parsing.Internal;

internal sealed class ListConverter<T>(IConverter<T> elementConverter, char delimiter) : IConverter<IReadOnlyList<T>>
{
    private readonly IConverter<T> _elementConverter = elementConverter;
    private readonly char _delimiter = delimiter;

    public string Description => _elementConverter.Description is { } element
        ? $"A list separated by '{_delimiter}'. Each item: {element}"
        : $"A list separated by '{_delimiter}'.";

    public bool TryParse(string input, out IReadOnlyList<T> result)
    {
        var list = new List<T>();
        result = list;

        foreach (var segment in Split(input, _delimiter))
        {
            if (!_elementConverter.TryParse(segment, out var element))
            {
                result = [];
                return false;
            }

            list.Add(element);
        }

        return true;
    }

    public string Format(IReadOnlyList<T> value) => string.Join(_delimiter, value.Select(_elementConverter.Format));

    // The trimmed items of a delimited list. Empty input is an empty list.
    internal static string[] Split(string input, char delimiter)
    {
        var trimmed = input.Trim();
        return trimmed.Length == 0 ? [] : [.. trimmed.Split(delimiter).Select(segment => segment.Trim())];
    }
}
