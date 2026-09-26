using Leander.Configuration.Internal;

namespace Leander.Configuration;

// Optional() for reference types: T becomes the annotated T?, and a missing value is null instead of an error.
// At runtime T? is T, so IsOptional is what tells them apart.
public static class OptionalReferenceExtensions
{
    public static ConfigurationDefinition<T?> Optional<T>(this ConfigurationDefinition<T> definition)
        where T : class =>
        definition.ToOptional(new OptionalReferenceReader<T>(definition));
}
