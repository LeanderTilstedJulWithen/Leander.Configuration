using Leander.Configuration.Internal;

namespace Leander.Configuration;

public static class ValueSource
{
    public const char KeySeparator = ':';

    // Uses the dictionary as-is, without copying. keyComparer must be the comparer the dictionary uses,
    // so that child names are matched the same way as values are looked up.
    public static IValueSource FromDictionary(IReadOnlyDictionary<string, string?> values, StringComparer keyComparer) =>
        new DictionaryValueSource(values, keyComparer);

    // Keys are compared case-insensitively, matching Microsoft.Extensions.Configuration.
    // When keys differ only in case, the last one wins.
    public static IValueSource FromPairs(IEnumerable<KeyValuePair<string, string?>> values)
    {
        var dictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            dictionary[key] = value;
        }

        return new DictionaryValueSource(dictionary, StringComparer.OrdinalIgnoreCase);
    }
}
