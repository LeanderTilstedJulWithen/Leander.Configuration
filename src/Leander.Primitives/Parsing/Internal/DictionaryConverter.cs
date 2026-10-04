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

    // Every entry is checked, and every reason is reported, e.g. "entry 1: value: 'x' is not a valid Int32". Entries count from 0.
    public bool TryParse(string input, out IReadOnlyDictionary<TKey, TValue> result, out IReadOnlyList<IFormattableText<string>> errors)
    {
        var dictionary = new Dictionary<TKey, TValue>();
        var reasons = new List<IFormattableText<string>>();
        errors = reasons;
        result = dictionary;

        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        var entries = trimmed.Split(_entryDelimiter);
        for (var index = 0; index < entries.Length; index++)
        {
            var prefix = $"entry {index}: ";
            var entry = entries[index].Trim();
            var parts = entry.Split(_keyValueDelimiter, 2);
            if (parts.Length != 2)
            {
                reasons.Add(FormattableText.Create<string>(formatter => $"{prefix}{formatter.Format($"'{entry}'")} has no '{_keyValueDelimiter}'"));
                continue;
            }

            var keyText = parts[0].Trim();
            var valueText = parts[1].Trim();

            var hasKey = _keyConverter.TryParse(keyText, out var key, out var keyErrors);
            if (!hasKey)
            {
                reasons.AddRange(PartErrors.Of<TKey>($"{prefix}key: ", keyText, keyErrors));
            }

            var hasValue = _valueConverter.TryParse(valueText, out var value, out var valueErrors);
            if (!hasValue)
            {
                reasons.AddRange(PartErrors.Of<TValue>($"{prefix}value: ", valueText, valueErrors));
            }

            if (hasKey && hasValue && !dictionary.TryAdd(key, value))
            {
                reasons.Add(FormattableText.Create<string>(formatter => $"{prefix}duplicate key {formatter.Format($"'{keyText}'")}"));
            }
        }

        if (reasons.Count > 0)
        {
            result = new Dictionary<TKey, TValue>();
            return false;
        }

        return true;
    }
}
