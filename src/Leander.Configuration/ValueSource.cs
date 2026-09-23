using Leander.Configuration.Internal;

namespace Leander.Configuration;

public static class ValueSource
{
    public const char KeySeparator = ':';

    // Keys are compared case-insensitively, matching Microsoft.Extensions.Configuration.
    public static IValueSource FromDictionary(IEnumerable<KeyValuePair<string, string?>> values) =>
        new DictionaryValueSource(values);
}
