using Leander.Configuration.Internal;

namespace Leander.Configuration;

/// <summary>
/// Creates <see cref="IValueSource"/> instances from keys and values.
/// </summary>
public static class ValueSource
{
    /// <summary>
    /// Separates the sections of a key, e.g. <c>Server:Port</c>.
    /// </summary>
    public const char KeySeparator = ':';

    /// <summary>
    /// Creates a source that uses <paramref name="values"/> as it is, without copying.
    /// </summary>
    /// <param name="values">The values by key.</param>
    /// <param name="keyComparer">
    /// The comparer the dictionary uses, so that child names are matched the same way as values are looked up.
    /// </param>
    public static IValueSource FromDictionary(IReadOnlyDictionary<string, string?> values, StringComparer keyComparer) =>
        new DictionaryValueSource(values, keyComparer);

    /// <summary>
    /// Creates a source from a copy of <paramref name="values"/>.
    /// </summary>
    /// <remarks>
    /// Keys are compared case-insensitively, matching Microsoft.Extensions.Configuration.
    /// When keys differ only in case, the last one wins.
    /// </remarks>
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
