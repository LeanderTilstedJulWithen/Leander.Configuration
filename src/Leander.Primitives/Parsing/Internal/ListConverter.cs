namespace Leander.Primitives.Parsing.Internal;

internal sealed class ListConverter<T>(IConverter<T> elementConverter, char delimiter) : IConverter<IReadOnlyList<T>>
{
    public bool TryParse(string input, out IReadOnlyList<T> result)
    {
        var list = new List<T>();
        result = list;

        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        foreach (var segment in trimmed.Split(delimiter))
        {
            if (!elementConverter.TryParse(segment.Trim(), out var element))
            {
                result = [];
                return false;
            }

            list.Add(element);
        }

        return true;
    }

    public string Format(IReadOnlyList<T> value) => string.Join(delimiter, value.Select(elementConverter.Format));
}
