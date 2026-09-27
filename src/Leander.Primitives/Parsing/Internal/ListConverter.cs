namespace Leander.Primitives.Parsing.Internal;

internal sealed class ListConverter<T>(IConverter<T> elementConverter, char delimiter) : IConverter<IReadOnlyList<T>>
{
    private readonly IConverter<T> _elementConverter = elementConverter;
    private readonly char _delimiter = delimiter;

    public bool TryParse(string input, out IReadOnlyList<T> result)
    {
        var list = new List<T>();
        result = list;

        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        foreach (var segment in trimmed.Split(_delimiter))
        {
            if (!_elementConverter.TryParse(segment.Trim(), out var element))
            {
                result = [];
                return false;
            }

            list.Add(element);
        }

        return true;
    }

    public string Format(IReadOnlyList<T> value) => string.Join(_delimiter, value.Select(_elementConverter.Format));
}
