namespace Leander.Primitives.Parsing.Internal;

internal sealed class DictionaryConverter<TKey, TValue>(
    IConverter<TKey> keyConverter,
    IConverter<TValue> valueConverter,
    char entryDelimiter,
    char keyValueDelimiter) : IConverter<IReadOnlyDictionary<TKey, TValue>> where TKey : notnull
{
    public bool TryParse(string input, out IReadOnlyDictionary<TKey, TValue> result)
    {
        var dictionary = new Dictionary<TKey, TValue>();
        result = dictionary;

        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        foreach (var entry in trimmed.Split(entryDelimiter))
        {
            var parts = entry.Split(keyValueDelimiter, 2);
            if (parts.Length != 2 ||
                !keyConverter.TryParse(parts[0].Trim(), out var key) ||
                !valueConverter.TryParse(parts[1].Trim(), out var value) ||
                !dictionary.TryAdd(key, value))
            {
                result = new Dictionary<TKey, TValue>();
                return false;
            }
        }

        return true;
    }

    public string Format(IReadOnlyDictionary<TKey, TValue> value) => string.Join(
        entryDelimiter,
        value.Select(pair => $"{keyConverter.Format(pair.Key)}{keyValueDelimiter}{valueConverter.Format(pair.Value)}"));
}
