namespace Leander.Configuration.Internal;

// Reads the dictionary as-is; keyComparer must be the comparer the dictionary itself uses.
internal sealed class DictionaryValueSource(IReadOnlyDictionary<string, string?> values, StringComparer keyComparer) : IValueSource
{
    private readonly IReadOnlyDictionary<string, string?> _values = values;
    private readonly StringComparer _keyComparer = keyComparer;

    public string? GetValue(string key) => _values.GetValueOrDefault(key);

    public IReadOnlyList<string> GetChildNames(string key)
    {
        var names = new List<string>();
        var seen = new HashSet<string>(_keyComparer);

        foreach (var candidate in _values.Keys)
        {
            if (candidate.Length <= key.Length ||
                candidate[key.Length] != ValueSource.KeySeparator ||
                !_keyComparer.Equals(candidate[..key.Length], key))
            {
                continue;
            }

            var remainder = candidate[(key.Length + 1)..];
            var separator = remainder.IndexOf(ValueSource.KeySeparator);
            var name = separator < 0 ? remainder : remainder[..separator];

            if (seen.Add(name))
            {
                names.Add(name);
            }
        }

        return names;
    }
}
