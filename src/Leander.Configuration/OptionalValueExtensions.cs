using Leander.Configuration.Internal;

namespace Leander.Configuration;

// Optional() for value types: T becomes Nullable<T>, and a missing value is null instead of an error.
// C# does not overload on constraints alone, so reference types get their own class.
public static class OptionalValueExtensions
{
    public static ConfigurationDefinition<T?> Optional<T>(this ConfigurationDefinition<T> definition)
        where T : struct =>
        definition.ToOptional(new OptionalValueReader<T>(definition));
}
