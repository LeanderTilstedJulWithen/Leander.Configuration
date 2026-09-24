namespace Leander.Configuration.Internal;

// Reads the dictionary as-is; keyComparer must be the comparer the dictionary itself uses.
internal sealed class DictionaryValueSource(IReadOnlyDictionary<string, string?> values, StringComparer keyComparer) : IValueSource
{
    public string? GetValue(string key) => values.GetValueOrDefault(key);

    public IReadOnlyList<string> GetChildNames(string key)
    {
        var names = new List<string>();
        var seen = new HashSet<string>(keyComparer);

        foreach (var candidate in values.Keys)
        {
            if (candidate.Length <= key.Length ||
                candidate[key.Length] != ValueSource.KeySeparator ||
                !keyComparer.Equals(candidate[..key.Length], key))
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
