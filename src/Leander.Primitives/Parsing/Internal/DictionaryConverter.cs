namespace Leander.Primitives.Parsing.Internal;

internal sealed class DictionaryConverter<TKey, TValue>(
    IConverter<TKey> keyConverter,
    IConverter<TValue> valueConverter,
    char entryDelimiter,
    char keyValueDelimiter) : IConverter<IReadOnlyDictionary<TKey, TValue>> where TKey : notnull
{
    private readonly char _entryDelimiter = entryDelimiter;
    private readonly IConverter<TKey> _keyConverter = keyConverter;
    private readonly char _keyValueDelimiter = keyValueDelimiter;
    private readonly IConverter<TValue> _valueConverter = valueConverter;

    public string Description
    {
        get
        {
            var description = $"Entries separated by '{_entryDelimiter}', each a key and a value separated by '{_keyValueDelimiter}'.";
            if (_keyConverter.Description is { } key)
            {
                description += $" Each key: {key}";
            }

            if (_valueConverter.Description is { } value)
            {
                description += $" Each value: {value}";
            }

            return description;
        }
    }

    public string Format(IReadOnlyDictionary<TKey, TValue> value) => string.Join(
        _entryDelimiter,
        value.Select(pair => $"{_keyConverter.Format(pair.Key)}{_keyValueDelimiter}{_valueConverter.Format(pair.Value)}"));

    public bool TryParse(string input, out IReadOnlyDictionary<TKey, TValue> result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        errors = [];
        var dictionary = new Dictionary<TKey, TValue>();
        result = dictionary;

        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        foreach (var entry in trimmed.Split(_entryDelimiter))
        {
            var parts = entry.Split(_keyValueDelimiter, 2);
            if (parts.Length != 2 ||
                !_keyConverter.TryParse(parts[0].Trim(), out var key) ||
                !_valueConverter.TryParse(parts[1].Trim(), out var value) ||
                !dictionary.TryAdd(key, value))
            {
                result = new Dictionary<TKey, TValue>();
                return false;
            }
        }

        return true;
    }
}
