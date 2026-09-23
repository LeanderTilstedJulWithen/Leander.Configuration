namespace Leander.Configuration.Internal;

internal sealed class DictionaryValueSource : IValueSource
{
    private readonly Dictionary<string, string?> _values;

    public DictionaryValueSource(IEnumerable<KeyValuePair<string, string?>> values)
    {
        _values = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
    }

    public string? GetValue(string key) => _values.GetValueOrDefault(key);

    public IReadOnlyList<string> GetChildNames(string key)
    {
        var prefix = key + ValueSource.KeySeparator;
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in _values.Keys)
        {
            if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var remainder = candidate[prefix.Length..];
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
